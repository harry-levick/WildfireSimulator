using System;
using System.Collections;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using UnityEngine;

public class FireBehaviour : MonoBehaviour
{
    public AbstractMap Map;
    public Vector3 IgnitionPoint;
    Vector3 North = new Vector3(0, 0, 1);
    static readonly HttpClient client = new HttpClient();
    private const string ModelNumberUrl =
        "http://127.0.0.1:5000/model-number?lat={0}&lon={1}";
    private const string ModelParametersUrl =
        "http://127.0.0.1:5000/model-parameters?number={0}";
    private const string MoistureUrl =
        "http://127.0.0.1:5500/live-fuel-moisture-content?lat={0}&lon={1}";
    private const string WindSpeedUrl =
        "http://127.0.0.1:6000/weather-data?lat={0}&lon={1}";

    private bool Active = false;
    private FireController _controller;
    private float HoursPassed = 1;

    private double LengthWidthRatio;
    private double Eccentricity;
    private double HeadingFireRateOfSpread;
    private double HeadingFireBearing;
    private double BackingFireRateOfSpread;
    private double BackingFireBearing;

    private Hashtable directions = new Hashtable();

    private void Awake()
    {
        Map = FindObjectOfType<AbstractMap>();
    }

    public void Activate(Vector3 ignitionPoint, ref FireController controller)
    {
        IgnitionPoint = ignitionPoint;
        _controller = controller;

        var maxSpread = RateOfMaximumSpreadInMetresPerMinute(IgnitionPoint).GetAwaiter().GetResult();
        HeadingFireRateOfSpread = maxSpread.spreadRate;
        HeadingFireBearing = maxSpread.spreadBearing;

        if (HeadingFireRateOfSpread == 0.0)
        {
            print("Spread rate: 0");
            return;
        }

        LengthWidthRatio = 1 + (0.25 * EffectiveMidflameWindSpeed(IgnitionPoint).GetAwaiter().GetResult());
        Eccentricity = Math.Pow((Math.Pow(LengthWidthRatio, 2.0) - 1.0), 0.5) / LengthWidthRatio;

        BackingFireRateOfSpread = HeadingFireRateOfSpread * ((1 - Eccentricity) / (1 + Eccentricity));
        BackingFireBearing = ReverseBearing(HeadingFireBearing);

        for (int angle = 0; angle <= 170; angle += 10)
        {
            if (angle == 0) { directions.Add(angle, HeadingFireRateOfSpread); }
            else
            {
                var spreadRate = HeadingFireRateOfSpread * ((1 - Eccentricity) / (1 - (Eccentricity * Math.Cos(DegreesToRadians(angle)))));
                directions.Add(angle, spreadRate);
            }
        }

        Debug.DrawRay(IgnitionPoint, Vector3.up * 200, Color.green, 1000f);

        Active = true;
    }

    // Update is called once per frame
    private void Update()
    {
        if (!Active) return;
    }

    private void FixedUpdate()
    {
        if (!Active) return;
        CalculateSpread();
    }

    private void CalculateSpread()
    {
        print($"Hours Passed: {HoursPassed}");
        // loop through each direction (360 degrees) from the ignition point
        foreach(DictionaryEntry entry in directions)
        {
            double clockwiseAngle = ((int)entry.Key) + HeadingFireBearing;
            double anticlockwiseAngle = HeadingFireBearing - ((int)entry.Key);

            double spreadRate = (double) entry.Value;

            if (clockwiseAngle >= 360.0) { clockwiseAngle -= 360.0; }
            clockwiseAngle = DegreesToRadians(clockwiseAngle);
            if (anticlockwiseAngle < 0.0) { anticlockwiseAngle += 360.0; }
            anticlockwiseAngle = DegreesToRadians(anticlockwiseAngle);

            double DH_angle = spreadRate * HoursPassed;
            Vector2 clockwisePos = new Vector2(
                                (float)(IgnitionPoint.x + (DH_angle * Math.Sin(clockwiseAngle))),
                                (float)(IgnitionPoint.z + (DH_angle * Math.Cos(clockwiseAngle)))
                                );

            Debug.DrawRay(new Vector3(clockwisePos.x, IgnitionPoint.y, clockwisePos.y), Vector3.up * 100, Color.red, 1000f);

            Vector2 anticlockwisePos = new Vector2(
                                (float)(IgnitionPoint.x + (DH_angle * Math.Sin(anticlockwiseAngle))),
                                (float)(IgnitionPoint.z + (DH_angle * Math.Cos(anticlockwiseAngle)))
                                );

            Debug.DrawRay(new Vector3(anticlockwisePos.x, IgnitionPoint.y, anticlockwisePos.y), Vector3.up * 100, Color.red, 1000f);
        }

        double DB = BackingFireRateOfSpread * HoursPassed;
        Vector2 backingPos = new Vector2(
                            (float) (IgnitionPoint.x + (DB * Math.Sin(DegreesToRadians(BackingFireBearing)))),
                            (float) (IgnitionPoint.z + (DB * Math.Cos(DegreesToRadians(BackingFireBearing))))
                            );

        Debug.DrawRay(new Vector3(backingPos.x, IgnitionPoint.y, backingPos.y), Vector3.up * 100, Color.blue, 1000f);

        HoursPassed += _controller.HourIncrement * 60f;
    }

    /// <summary>
    /// Returns the rate of spread of fire in m/min given no wind or slope.
    /// </summary>
    /// <param name="point">the unity point in game space</param>
    /// <returns></returns>
    private async Task<double> ZeroWindZeroSlopeRateOfSpreadInMetresPerMin(Vector3 point)
    {
        FuelModel model = await FuelModelParameters(point);
        double fuelMoisture = await FuelMoistureContent(point);

        double propFluxNoWindSlope = await PropagatingFluxNoWindSlope(point);

        double heatSink = HeatSink(fuelMoisture, model);

        if (propFluxNoWindSlope == 0 || heatSink == 0) { return 0; }

        return FeetToMetres(propFluxNoWindSlope / heatSink);
    }

    private async Task<SpreadModel> RateOfMaximumSpreadInMetresPerMinute(Vector3 point)
    {
        WeatherModel weatherModel = await MidflameWindSpeed(point);
        Wind currentWind = weatherModel.current;

        double r0 = await ZeroWindZeroSlopeRateOfSpreadInMetresPerMin(point);
        double slopeBearing = GetSlopeBearingInDegrees(GetHitInfo(point));
        double windBearing = currentWind.wind_deg;

        /* for elapsed time t, the slope vector has magnitude Ds and direction 0.
         * The wind vector has magnitude Dw in direction w from the upslope.
         * the slope vector is (Ds, 0) and the wind vector is (Dwcosw, Dwsinw). 
         * The resultant vector is then (Ds + Dwcosw, Dwsinw). 
         * The magnitude of the head fire vector is Dh in direction a.
         */
        double Ds = r0 * await SlopeFactor(point);
        double Dw = r0 * await WindFactor(point);
        double w = Math.Abs(slopeBearing - windBearing);

        double X = Ds + (Dw * Math.Cos(DegreesToRadians(w)));
        double Y = Dw * Math.Sin(DegreesToRadians(w));
        double Dh = (double) Math.Pow(Math.Pow(X, 2f) + Math.Pow(Y, 2f), 0.5f);

        double a;
        if (Y == 0f || Dh == 0f) { a = 0f; }
        else { a = Math.Asin(DegreesToRadians(Math.Abs(Y) / Dh)); }
        
        // calculate a relative to North bearing
        if (slopeBearing >= windBearing)
        {
            a = slopeBearing - a;
        } else
        {
            a += slopeBearing;
        }
        double Rh = r0 + (Dh / 1f); // t = 1

        return new SpreadModel(Rh, a);
    }

    #region Heat Sink
    /// <summary>
    /// </summary>
    /// <param name="Mf">moisture content</param>
    /// <param name="model">fuel model</param>
    /// <returns></returns>
    private double HeatSink(double Mf, FuelModel model)
    {
        return model.mean_bulk_density *
                EffectiveHeatingNumber(model) *
                HeatOfPreignition(Mf);
    }

    /// <summary>
    /// </summary>
    /// <param name="sigma">surface-area-to-volume-ratio</param>
    /// <returns></returns>
    private double EffectiveHeatingNumber(FuelModel model)
    {
        if (model.characteristic_sav == 0.0) { return 0.0; }

        return Math.Exp(- 138 / model.characteristic_sav);
    }

    /// <summary>
    /// </summary>
    /// <param name="Mf">moisture content</param>
    /// <returns></returns>
    private double HeatOfPreignition(double Mf)
    {
        return 250 + (1116 * Mf);
    }
    #endregion

    #region Heat Source
    /// <summary>
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    private async Task<double> HeatSource(Vector3 point)
    {
        double propFlux = await PropagatingFluxNoWindSlope(point);

        return propFlux * (1f + await SlopeFactor(point) + await WindFactor(point));
    }

    /// <summary>
    /// </summary>
    /// <param name="point"></param>
    /// <returns>no-wind, no-slope propagating flux</returns>
    private async Task<double> PropagatingFluxNoWindSlope(Vector3 point)
    {
        FuelModel model = await FuelModelParameters(point);
        return await ReactionIntensity(point) * PropagatingFluxRatio(model);
    }

    /// <summary>
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    private async Task<double> ReactionIntensity(Vector3 point)
    {
        double Mf = await FuelMoistureContent(point);
        FuelModel model = await FuelModelParameters(point);
        double wn = NetFuelLoad(model.oven_dry_fuel_load);
        double nM = MoistureDampingCoefficient(Mf, model.dead_fuel_moisture_of_extinction);
        double g = NetFuelLoadWeightingFactor(model);

        return OptimumReactionVelocity(model) *
                (wn * g) *
                FuelModel.heat_content *
                nM *
                MineralDampingCoefficient();
    }

    /// <summary>
    /// </summary>
    /// <param name="model">fuel model</param>
    /// <returns></returns>
    private double OptimumReactionVelocity(FuelModel model)
    {
        double A;

        if (model.characteristic_sav == 0f) { A = 00; }
        else { A = 133 * Math.Pow(model.characteristic_sav, -0.7913); }

        if (model.relative_packing_ratio == 0.0) { return 0.0; }

        return MaximumReactionVelocity(model.characteristic_sav) *
                Math.Pow(model.relative_packing_ratio, A) *
                Math.Exp(A * (1.0 - model.relative_packing_ratio));
    }

    /// <summary>
    /// </summary>
    /// <param name="sigma">surface-area-to-volume-ratio</param>
    /// <returns></returns>
    private double MaximumReactionVelocity(double sigma)
    {
        if (sigma == 0f) { return 0; }

        return Math.Pow(sigma, 1.5) *
                Math.Pow(495f + (0.0594 * Math.Pow(sigma, 1.5)), -1.0);
    }

    /// <summary>
    /// </summary>
    /// <param name="model">fuel model</param>
    /// <returns></returns>
    private double PropagatingFluxRatio(FuelModel model)
    {
        double beta = MeanPackingRatio(model);

        return Math.Pow(192.0 + (0.2595 * model.characteristic_sav), -1.0) *
            Math.Exp((0.792 + (0.681 * Math.Pow(model.characteristic_sav, 0.5))) * (beta + 0.1));
    }

    /// <summary>
    /// </summary>
    /// <param name="w0">oven dry fuel load</param>
    /// <param name="sT">total mineral content</param>
    /// <returns></returns>
    private double NetFuelLoad(double w0)
    {
        return w0 * (1.0 - FuelModel.total_mineral_content);
    }

    private double NetFuelLoadWeightingFactor(FuelModel model)
    {
        if (model.characteristic_sav < 16) { return 0.0; }

        return 1.0;
    }

    /// <summary>
    /// </summary>
    /// <param name="Se">effective mineral content</param>
    /// <returns></returns>
    private double MineralDampingCoefficient()
    {
        double coefficient = 0.174 * Math.Pow(FuelModel.effective_mineral_content, -0.19);
        return Math.Min(coefficient, 1.0); // (max = 1)
    }

    /// <summary>
    /// </summary>
    /// <param name="Mf">moisture content</param>
    /// <param name="Mx">dead fuel moisture of extinction</param>
    /// <returns></returns>
    private double MoistureDampingCoefficient(double Mf, double Mx)
    {
        double rM = Math.Min((Mf / Mx), 1.0); // (max = 1)

        if (rM == 1.0) { return 0.0; }


        var coef = (1.0 - (2.59 * rM)) +
                (5.11 * Math.Pow(rM, 2.0)) -
                (3.52 * Math.Pow(rM, 3.0));

        return (1.0 - (2.59 * rM)) +
                (5.11 * Math.Pow(rM, 2.0)) -
                (3.52 * Math.Pow(rM, 3.0));
    }

    /// <summary>
    /// </summary>
    /// <param name="model">fuel model</param>
    /// <returns></returns>
    private double MeanPackingRatio(FuelModel model)
    {
        if (model.fuel_bed_depth == 0.0 || model.oven_dry_fuel_load == 0.0) { return 0.0; }

        return (1.0 / model.fuel_bed_depth) * (model.oven_dry_fuel_load / FuelModel.particle_density);
    }

    #endregion

    #region Environmental

    /// <summary>
    /// Rate of spread is modelled as constant for wind speeds
    /// greater than the maximum reliable wind speed.
    /// </summary>
    /// <param name="iR">reaction intensity</param>
    /// <returns></returns>
    private double MaximumReliableWindSpeed(double iR)
    {
        return 0.9 * iR;
    }

    /// <summary>
    /// </summary>
    /// <param name="point">the point in space to find the slope factor</param>
    /// <returns></returns>
    private async Task<double> SlopeFactor(Vector3 point)
    {
        FuelModel model = await FuelModelParameters(point);
        RaycastHit hitInfo = GetHitInfo(point);
        double theta = GetSlopeInDegrees(hitInfo);

        if (model.packing_ratio == 0) { return 0.0; }
        var slopeFactor = 5.27 * Math.Pow(model.packing_ratio, -0.3) * Math.Pow(Math.Tan(DegreesToRadians(theta)), 2);

        return slopeFactor;
    }

    /// <summary>
    /// phi_w
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    private async Task<double> WindFactor(Vector3 point)
    {
        WeatherModel weatherModel = await MidflameWindSpeed(point);
        double currentWindSpeed = weatherModel.current.wind_speed;
        FuelModel model = await FuelModelParameters(point);

        if (model.relative_packing_ratio == 0f) { return 0f; }

        double C = 7.47 * Math.Exp(-0.133 * Math.Pow(model.characteristic_sav, 0.55));
        double B = 0.025256 * Math.Pow(model.characteristic_sav, 0.54);
        double E = 0.715 * Math.Exp(-3.59 * model.characteristic_sav * Math.Pow(10, -4));
        double U =
                Math.Min(
                    MaximumReliableWindSpeed(await ReactionIntensity(point)),
                    currentWindSpeed
                );

        return (double)(C * Math.Pow(U, B) * Math.Pow(model.relative_packing_ratio, -E));
    }

    /// <summary>
    /// U_E
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    private async Task<double> EffectiveMidflameWindSpeed(Vector3 point)
    {
        double effectiveWindFactor = WindFactor(point).GetAwaiter().GetResult() +
                                    SlopeFactor(point).GetAwaiter().GetResult();

        FuelModel model = await FuelModelParameters(point);

        double B = 0.025256 * Math.Pow(model.characteristic_sav, 0.54f);
        double E = 0.715 * Math.Exp(-3.59f * model.characteristic_sav * Math.Pow(10f, -4f));
        double C = 7.47 * Math.Exp(-0.133 * Math.Pow(model.characteristic_sav, 0.55));

        return Math.Pow(effectiveWindFactor * Math.Pow(model.relative_packing_ratio, E) / C, -B);
    }

    double GetSlopeInDegrees(RaycastHit hitInfo)
    {
        Vector3 normal = hitInfo.normal;
        return Vector3.Angle(normal, Vector3.up);
    }

    double GetSlopeBearingInDegrees(RaycastHit hitInfo)
    {
        Vector3 normal = hitInfo.normal;

        Vector3 left = Vector3.Cross(normal, Vector3.down);
        Vector3 upslope = Vector3.Cross(normal, left);
        Vector3 upslopeFlat = new Vector3(upslope.x, 0, upslope.z).normalized;

        return BearingBetweenInDegrees(North, upslopeFlat);
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

        Vector2d latlon = Map.WorldToGeoPosition(point);

        response = client.GetAsync(string.Format(ModelNumberUrl, latlon.x, latlon.y)).Result;
        response.EnsureSuccessStatusCode();
        string modelNumber = await response.Content.ReadAsStringAsync();
        return Int32.Parse(modelNumber);
    }

    async Task<FuelModel> FuelModelParameters(Vector3 point)
    {
        HttpResponseMessage response;

        int modelNumber = await FuelModelNumber(point);
        response = client.GetAsync(string.Format(ModelParametersUrl, modelNumber)).Result;
        response.EnsureSuccessStatusCode();

        return JsonUtility.FromJson<FuelModel>(await response.Content.ReadAsStringAsync());
    }

    async Task<double> FuelMoistureContent(Vector3 point)
    {
        HttpResponseMessage response;
        Vector2d latlon = Map.WorldToGeoPosition(point);

        response = client.GetAsync(string.Format(MoistureUrl, latlon.x, latlon.y)).Result;
        response.EnsureSuccessStatusCode();
        return
            double.Parse(
                await response.Content.ReadAsStringAsync(),
                CultureInfo.InvariantCulture.NumberFormat
            ) / 100; // divide by 100 to get percentage fuel moisture content.
    }

    async Task<WeatherModel> MidflameWindSpeed(Vector3 point)
    {
        HttpResponseMessage response;
        Vector2d latlon = Map.WorldToGeoPosition(point);

        response = client.GetAsync(string.Format(WindSpeedUrl, latlon.x, latlon.y)).Result;
        response.EnsureSuccessStatusCode();
        string jsonString = await response.Content.ReadAsStringAsync();

        var jObject = JsonUtility.FromJson<WeatherModel>(jsonString);
        return jObject;
    }
    #endregion
}
