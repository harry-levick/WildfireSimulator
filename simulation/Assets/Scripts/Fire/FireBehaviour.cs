using System;
using System.Collections;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using Player.Model;
using UnityEngine;

namespace Fire
{
    public class FireBehaviour
    {
        private readonly AbstractMap Map;
        private readonly Vector3 _ignitionPoint;
        readonly Vector3 _north = new Vector3(0, 0, 1);
        private static readonly HttpClient Client = new HttpClient();
        private const string ModelNumberUrl =
            "http://127.0.0.1:5000/model-number?lat={0}&lon={1}";
        private const string ModelParametersUrl =
            "http://127.0.0.1:5000/model-parameters?number={0}";
        private const string MoistureUrl =
            "http://127.0.0.1:5500/live-fuel-moisture-content?lat={0}&lon={1}";
        private const string WindSpeedUrl =
            "http://127.0.0.1:6000/weather-data?lat={0}&lon={1}";

        private bool _active = false;
        private float _hoursPassed = 1;

        private double _lengthWidthRatio;
        private double _eccentricity;
        private double _headingFireRateOfSpread;
        private double _headingFireBearing;
        private double _backingFireRateOfSpread;
        private double _backingFireBearing;

        private Hashtable _directions = new Hashtable();
        
        public FireBehaviour(AbstractMap map, Vector3 ignitionPoint)
        {
            Map = map;
            _ignitionPoint = ignitionPoint;
        }

        public bool CanBurn()
        {
            return RateOfMaximumSpreadInMetresPerMinute(_ignitionPoint)
                .GetAwaiter()
                .GetResult()
                .spreadRate > 0.0;
        }

        public void Ignite()
        {
            var maxSpread = RateOfMaximumSpreadInMetresPerMinute(_ignitionPoint).GetAwaiter().GetResult();
            _headingFireRateOfSpread = maxSpread.spreadRate;
            _headingFireBearing = maxSpread.spreadBearing;

            if (_headingFireRateOfSpread == 0.0) return; // can't start a fire here

            _lengthWidthRatio = 1 + (0.25 * EffectiveMidflameWindSpeed(_ignitionPoint).GetAwaiter().GetResult());
            _eccentricity = Math.Pow((Math.Pow(_lengthWidthRatio, 2.0) - 1.0), 0.5) / _lengthWidthRatio;

            _backingFireRateOfSpread = _headingFireRateOfSpread * ((1 - _eccentricity) / (1 + _eccentricity));
            _backingFireBearing = ReverseBearing(_headingFireBearing);

            for (var angle = 0; angle <= 170; angle += 10)
            {
                if (angle == 0) { _directions.Add(angle, _headingFireRateOfSpread); }
                else
                {
                    var spreadRate = _headingFireRateOfSpread * ((1 - _eccentricity) / (1 - (_eccentricity * Math.Cos(DegreesToRadians(angle)))));
                    _directions.Add(angle, spreadRate);
                }
            }

            Debug.DrawRay(_ignitionPoint, Vector3.up * 200, Color.green, 1000f);

            _active = true;
        }

        public void Pause()
        {
            _active = false;
        }

        public void Play()
        {
            if (!_active) _active = true;
        }

        // Update is called once per frame
        private void Update()
        {
            if (!_active) return;
        }

        private void FixedUpdate()
        {
            if (!_active) return;
            CalculateSpread();
        }

        private void CalculateSpread()
        {
            // loop through each direction (360 degrees) from the ignition point
            foreach(DictionaryEntry entry in _directions)
            {
                var clockwiseAngle = ((int)entry.Key) + _headingFireBearing;
                var anticlockwiseAngle = _headingFireBearing - ((int)entry.Key);

                var spreadRate = (double) entry.Value;

                if (clockwiseAngle >= 360.0) { clockwiseAngle -= 360.0; }
                clockwiseAngle = DegreesToRadians(clockwiseAngle);
                if (anticlockwiseAngle < 0.0) { anticlockwiseAngle += 360.0; }
                anticlockwiseAngle = DegreesToRadians(anticlockwiseAngle);

                var dhAngle = spreadRate * _hoursPassed;
                var clockwisePos = new Vector2(
                                    (float)(_ignitionPoint.x + (dhAngle * Math.Sin(clockwiseAngle))),
                                    (float)(_ignitionPoint.z + (dhAngle * Math.Cos(clockwiseAngle)))
                                    );

                Debug.DrawRay(new Vector3(clockwisePos.x, _ignitionPoint.y, clockwisePos.y), Vector3.up * 100, Color.red, 1000f);

                var anticlockwisePos = new Vector2(
                                    (float)(_ignitionPoint.x + (dhAngle * Math.Sin(anticlockwiseAngle))),
                                    (float)(_ignitionPoint.z + (dhAngle * Math.Cos(anticlockwiseAngle)))
                                    );

                Debug.DrawRay(new Vector3(anticlockwisePos.x, _ignitionPoint.y, anticlockwisePos.y), Vector3.up * 100, Color.red, 1000f);
            }

            var db = _backingFireRateOfSpread * _hoursPassed;
            var backingPos = new Vector2(
                                (float) (_ignitionPoint.x + (db * Math.Sin(DegreesToRadians(_backingFireBearing)))),
                                (float) (_ignitionPoint.z + (db * Math.Cos(DegreesToRadians(_backingFireBearing))))
                                );

            Debug.DrawRay(new Vector3(backingPos.x, _ignitionPoint.y, backingPos.y), Vector3.up * 100, Color.blue, 1000f);

            // TODO: add method of changing time in the Hud Menu
            const float timeIncrementInHours = 0.5f;
            _hoursPassed += timeIncrementInHours * 60f;
        }

        /// <summary>
        /// Returns the rate of spread of fire in m/min given no wind or slope.
        /// </summary>
        /// <param name="point">the unity point in game space</param>
        /// <returns></returns>
        private async Task<double> ZeroWindZeroSlopeRateOfSpreadInMetresPerMin(Vector3 point)
        {
            var model = await FuelModelParameters(point);
            var fuelMoisture = await FuelMoistureContent(point);

            var propFluxNoWindSlope = await PropagatingFluxNoWindSlope(point);

            var heatSink = HeatSink(fuelMoisture, model);

            if (propFluxNoWindSlope == 0 || heatSink == 0) { return 0; }

            return FeetToMetres(propFluxNoWindSlope / heatSink);
        }

        private async Task<Spread> RateOfMaximumSpreadInMetresPerMinute(Vector3 point)
        {
            var weatherModel = await MidflameWindSpeed(point);
            var currentWind = weatherModel.current;

            var r0 = await ZeroWindZeroSlopeRateOfSpreadInMetresPerMin(point);
            var slopeBearing = GetSlopeBearingInDegrees(GetHitInfo(point));
            double windBearing = currentWind.wind_deg;

            /* for elapsed time t, the slope vector has magnitude Ds and direction 0.
             * The wind vector has magnitude Dw in direction w from the upslope.
             * the slope vector is (Ds, 0) and the wind vector is (Dwcosw, Dwsinw). 
             * The resultant vector is then (Ds + Dwcosw, Dwsinw). 
             * The magnitude of the head fire vector is Dh in direction a.
             */
            var ds = r0 * await SlopeFactor(point);
            var dw = r0 * await WindFactor(point);
            var w = Math.Abs(slopeBearing - windBearing);

            var x = ds + (dw * Math.Cos(DegreesToRadians(w)));
            var y = dw * Math.Sin(DegreesToRadians(w));
            var dh = (double) Math.Pow(Math.Pow(x, 2f) + Math.Pow(y, 2f), 0.5f);

            double a;
            if (y == 0f || dh == 0f) { a = 0f; }
            else { a = Math.Asin(DegreesToRadians(Math.Abs(y) / dh)); }
        
            // calculate a relative to North bearing
            if (slopeBearing >= windBearing)
            {
                a = slopeBearing - a;
            } else
            {
                a += slopeBearing;
            }
            var rh = r0 + (dh / 1f); // t = 1

            return new Spread(rh, a);
        }

        #region Heat Sink
        /// <summary>
        /// </summary>
        /// <param name="Mf">moisture content</param>
        /// <param name="model">fuel model</param>
        /// <returns></returns>
        private double HeatSink(double Mf, Fuel model)
        {
            return model.mean_bulk_density *
                    EffectiveHeatingNumber(model) *
                    HeatOfPreignition(Mf);
        }

        /// <summary>
        /// </summary>
        /// <param name="sigma">surface-area-to-volume-ratio</param>
        /// <returns></returns>
        private double EffectiveHeatingNumber(Fuel model)
        {
            return model.characteristic_sav == 0.0 ? 0.0 : Math.Exp(- 138 / model.characteristic_sav);
        }

        /// <summary>
        /// </summary>
        /// <param name="mf">moisture content</param>
        /// <returns></returns>
        private static double HeatOfPreignition(double mf)
        {
            return 250 + (1116 * mf);
        }
        #endregion

        #region Heat Source
        /// <summary>
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private async Task<double> HeatSource(Vector3 point)
        {
            var propFlux = await PropagatingFluxNoWindSlope(point);

            return propFlux * (1f + await SlopeFactor(point) + await WindFactor(point));
        }

        /// <summary>
        /// </summary>
        /// <param name="point"></param>
        /// <returns>no-wind, no-slope propagating flux</returns>
        private async Task<double> PropagatingFluxNoWindSlope(Vector3 point)
        {
            var model = await FuelModelParameters(point);
            return await ReactionIntensity(point) * PropagatingFluxRatio(model);
        }

        /// <summary>
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private async Task<double> ReactionIntensity(Vector3 point)
        {
            var mf = await FuelMoistureContent(point);
            var model = await FuelModelParameters(point);
            var wn = NetFuelLoad(model.oven_dry_fuel_load);
            var nM = MoistureDampingCoefficient(mf, model.dead_fuel_moisture_of_extinction);
            var g = NetFuelLoadWeightingFactor(model);

            return OptimumReactionVelocity(model) *
                    (wn * g) *
                    Fuel.heat_content *
                    nM *
                    MineralDampingCoefficient();
        }

        /// <summary>
        /// </summary>
        /// <param name="model">fuel model</param>
        /// <returns></returns>
        private static double OptimumReactionVelocity(Fuel model)
        {
            double a;

            if (model.characteristic_sav == 0f) { a = 00; }
            else { a = 133 * Math.Pow(model.characteristic_sav, -0.7913); }

            if (model.relative_packing_ratio == 0.0) { return 0.0; }

            return MaximumReactionVelocity(model.characteristic_sav) *
                    Math.Pow(model.relative_packing_ratio, a) *
                    Math.Exp(a * (1.0 - model.relative_packing_ratio));
        }

        /// <summary>
        /// </summary>
        /// <param name="sigma">surface-area-to-volume-ratio</param>
        /// <returns></returns>
        private static double MaximumReactionVelocity(double sigma)
        {
            if (sigma == 0f) { return 0; }

            return Math.Pow(sigma, 1.5) *
                    Math.Pow(495f + (0.0594 * Math.Pow(sigma, 1.5)), -1.0);
        }

        /// <summary>
        /// </summary>
        /// <param name="model">fuel model</param>
        /// <returns></returns>
        private double PropagatingFluxRatio(Fuel model)
        {
            var beta = MeanPackingRatio(model);

            return Math.Pow(192.0 + (0.2595 * model.characteristic_sav), -1.0) *
                Math.Exp((0.792 + (0.681 * Math.Pow(model.characteristic_sav, 0.5))) * (beta + 0.1));
        }

        /// <summary>
        /// </summary>
        /// <param name="w0">oven dry fuel load</param>
        /// <param name="sT">total mineral content</param>
        /// <returns></returns>
        private static double NetFuelLoad(double w0)
        {
            return w0 * (1.0 - Fuel.total_mineral_content);
        }

        private static double NetFuelLoadWeightingFactor(Fuel model)
        {
            return model.characteristic_sav < 16 ? 0.0 : 1.0;
        }

        /// <summary>
        /// </summary>
        /// <param name="Se">effective mineral content</param>
        /// <returns></returns>
        private static double MineralDampingCoefficient()
        {
            var coefficient = 0.174 * Math.Pow(Fuel.effective_mineral_content, -0.19);
            return Math.Min(coefficient, 1.0); // (max = 1)
        }

        /// <summary>
        /// </summary>
        /// <param name="mf">moisture content</param>
        /// <param name="mx">dead fuel moisture of extinction</param>
        /// <returns></returns>
        private static double MoistureDampingCoefficient(double mf, double mx)
        {
            var rM = Math.Min((mf / mx), 1.0); // (max = 1)

            if (rM == 1.0) { return 0.0; }


            return (1.0 - (2.59 * rM)) +
                   (5.11 * Math.Pow(rM, 2.0)) -
                   (3.52 * Math.Pow(rM, 3.0));
        }

        /// <summary>
        /// </summary>
        /// <param name="model">fuel model</param>
        /// <returns></returns>
        private static double MeanPackingRatio(Fuel model)
        {
            if (model.fuel_bed_depth == 0.0 || model.oven_dry_fuel_load == 0.0) { return 0.0; }

            return (1.0 / model.fuel_bed_depth) * (model.oven_dry_fuel_load / Fuel.particle_density);
        }

        #endregion

        #region Environmental

        /// <summary>
        /// Rate of spread is modelled as constant for wind speeds
        /// greater than the maximum reliable wind speed.
        /// </summary>
        /// <param name="iR">reaction intensity</param>
        /// <returns></returns>
        private static double MaximumReliableWindSpeed(double iR)
        {
            return 0.9 * iR;
        }

        /// <summary>
        /// </summary>
        /// <param name="point">the point in space to find the slope factor</param>
        /// <returns></returns>
        private async Task<double> SlopeFactor(Vector3 point)
        {
            var model = await FuelModelParameters(point);
            var hitInfo = GetHitInfo(point);
            var theta = GetSlopeInDegrees(hitInfo);

            if (model.packing_ratio == 0) { return 0.0; }
            var slopeFactor = 5.27 * Math.Pow(model.packing_ratio, -0.3) * Math.Pow(Math.Tan(DegreesToRadians(theta)), 2);

            return slopeFactor;
        }

        /// <summary>
        /// phi_w
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private async Task<double> WindFactor(Vector3 point)
        {
            var weatherModel = await MidflameWindSpeed(point);
            double currentWindSpeed = weatherModel.current.wind_speed;
            var model = await FuelModelParameters(point);

            if (model.relative_packing_ratio == 0f) { return 0f; }

            var c = 7.47 * Math.Exp(-0.133 * Math.Pow(model.characteristic_sav, 0.55));
            var b = 0.025256 * Math.Pow(model.characteristic_sav, 0.54);
            var e = 0.715 * Math.Exp(-3.59 * model.characteristic_sav * Math.Pow(10, -4));
            var u =
                    Math.Min(
                        MaximumReliableWindSpeed(await ReactionIntensity(point)),
                        currentWindSpeed
                    );

            return (c * Math.Pow(u, b) * Math.Pow(model.relative_packing_ratio, -e));
        }

        /// <summary>
        /// U_E
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private async Task<double> EffectiveMidflameWindSpeed(Vector3 point)
        {
            var effectiveWindFactor = WindFactor(point).GetAwaiter().GetResult() +
                                      SlopeFactor(point).GetAwaiter().GetResult();

            var model = await FuelModelParameters(point);

            var b = 0.025256 * Math.Pow(model.characteristic_sav, 0.54f);
            var e = 0.715 * Math.Exp(-3.59f * model.characteristic_sav * Math.Pow(10f, -4f));
            var c = 7.47 * Math.Exp(-0.133 * Math.Pow(model.characteristic_sav, 0.55));

            return Math.Pow(effectiveWindFactor * Math.Pow(model.relative_packing_ratio, e) / c, -b);
        }

        private static double GetSlopeInDegrees(RaycastHit hitInfo)
        {
            var normal = hitInfo.normal;
            return Vector3.Angle(normal, Vector3.up);
        }

        private double GetSlopeBearingInDegrees(RaycastHit hitInfo)
        {
            var normal = hitInfo.normal;

            var left = Vector3.Cross(normal, Vector3.down);
            var upslope = Vector3.Cross(normal, left);
            var upslopeFlat = new Vector3(upslope.x, 0, upslope.z).normalized;

            return BearingBetweenInDegrees(_north, upslopeFlat);
        }
        #endregion

        #region utils

        double BearingBetweenInDegrees(Vector3 a, Vector3 b)
        {
            Vector3 normal = Vector3.up;
            // angle in [0, 180]
            double angle = Vector3.Angle(a, b);
            double sign = Math.Sign(Vector3.Dot(normal, Vector3.Cross(a, b)));

            // angle in [-179, 180]
            double signedAngle = angle * sign;

            // angle in [0, 360]
            double bearing = (signedAngle + 360) % 360;
            return bearing;
        }

        RaycastHit GetHitInfo(Vector3 point)
        {
            Vector3 origin = new Vector3(point.x, point.y + 100, point.z);

            RaycastHit hitInfo;
            if (Physics.Raycast(origin, Vector3.down, out hitInfo, int.MaxValue))
            {
                return hitInfo;
            }
            else throw new Exception("No Hit in Raycast.");
        }

        Vector3 GetVector3FromVector2(Vector3 point)
        {
            Vector2d latlon = Map.WorldToGeoPosition(new Vector3(point.x, 0, point.z));
            Vector3 newPoint = new Vector3(point.x, Map.QueryElevationInUnityUnitsAt(latlon), point.y);
            return newPoint;
        }

        private double DegreesToRadians(double deg)
        {
            return (Math.PI / 180f) * deg;
        }

        private double RadiansToDegrees(double rad)
        {
            return (180f / Math.PI) * rad;
        }

        private double FeetToMetres(double feet) => 0.3048 * feet;

        private double ReverseBearing(double forward)
        {
            if (forward >= 180)
            {
                return forward - 180f;
            }
            else
            {
                return forward + 180f;
            }
        }

        #endregion

        #region api calls
        async Task<int> FuelModelNumber(Vector3 point)
        {
            HttpResponseMessage response;

            var latlon = Map.WorldToGeoPosition(point);

            response = Client.GetAsync(string.Format(ModelNumberUrl, latlon.x, latlon.y)).Result;
            response.EnsureSuccessStatusCode();
            var modelNumber = await response.Content.ReadAsStringAsync();
            return int.Parse(modelNumber);
        }

        async Task<Fuel> FuelModelParameters(Vector3 point)
        {
            HttpResponseMessage response;

            var modelNumber = await FuelModelNumber(point);
            response = Client.GetAsync(string.Format(ModelParametersUrl, modelNumber)).Result;
            response.EnsureSuccessStatusCode();

            return JsonUtility.FromJson<Fuel>(await response.Content.ReadAsStringAsync());
        }

        async Task<double> FuelMoistureContent(Vector3 point)
        {
            HttpResponseMessage response;
            var latlon = Map.WorldToGeoPosition(point);

            response = Client.GetAsync(string.Format(MoistureUrl, latlon.x, latlon.y)).Result;
            response.EnsureSuccessStatusCode();
            return
                double.Parse(
                    await response.Content.ReadAsStringAsync(),
                    CultureInfo.InvariantCulture.NumberFormat
                ) / 100; // divide by 100 to get percentage fuel moisture content.
        }

        async Task<Weather> MidflameWindSpeed(Vector3 point)
        {
            HttpResponseMessage response;
            var latlon = Map.WorldToGeoPosition(point);

            response = Client.GetAsync(string.Format(WindSpeedUrl, latlon.x, latlon.y)).Result;
            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();

            var jObject = JsonUtility.FromJson<Weather>(jsonString);
            return jObject;
        }
        #endregion
    }
}
