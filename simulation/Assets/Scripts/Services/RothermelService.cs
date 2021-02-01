using Mapbox.Utils;
using Mapbox.Unity.Map;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Constants.DirectionConstants;

namespace Services
{
    public class RothermelService
    {
        
        private readonly AbstractMap _map;

        public RothermelService(AbstractMap map)
        {
            _map = map;
        }

        public Dictionary<Vector3, double> GetSpreadInCardinalDirectionsMetresPerMinute(Vector3 unityCoords, 
            Fuel model, Weather weather, double moistureContent)
        {
            return GetSpreadInCardinalDirectionsFeetPerMinute(unityCoords, model, weather, moistureContent)
                .ToDictionary(
                    kp => kp.Key, 
                    kp => FeetToMetres(kp.Value)
                );
        }
        
        public Dictionary<Vector3, double> GetSpreadInCardinalDirectionsFeetPerMinute(Vector3 unityCoords, 
            Fuel model, Weather weather, double moistureContent)
        {
            var cardinals = new Dictionary<Vector3, double>()
            {
                { NORTH_VECTOR, NORTH_BEARING }, { SOUTH_VECTOR, SOUTH_BEARING }, 
                { EAST_VECTOR, EAST_BEARING }, { WEST_VECTOR, WEST_BEARING }
            };

            var spread = new Dictionary<Vector3, double>();
            
            var maxSpread = RateOfMaximumSpreadInFeetPerMinute(unityCoords, weather.current, 
                    model, moistureContent);
            var windSpeed = EffectiveMidflameWindSpeed(unityCoords, weather.current, 
                    model, moistureContent);

            var lengthWidthRatio = 1 + (0.25 * windSpeed);
            var eccentricity = Math.Pow(Math.Pow(lengthWidthRatio, 2.0) - 1.0, 0.5) / lengthWidthRatio;

            foreach (var entry in cardinals)
            {
                var angle = Math.Abs(entry.Value - maxSpread.SpreadBearing);
                var spreadRate = maxSpread.SpreadRateFeetPerMin *
                                 ((1 - eccentricity) / (1 - eccentricity * Math.Cos(DegreesToRadians(angle))));
                
                spread[entry.Key] = spreadRate;
            }

            return spread;
        }
        
        public Vector2d GetLatLonFromUnityCoords(Vector3 point)
        {
            return _map.WorldToGeoPosition(point);
        }
        
        public Spread RateOfMaximumSpreadInFeetPerMinute(Vector3 point, Wind midflameWindSpeed, Fuel model, 
            double fuelMoistureContent)
        {
            var windBearing = midflameWindSpeed.wind_deg;
            var hitInfoAtPoint = GetHitInfo(point);
            
            var r0 = ZeroWindZeroSlopeRateOfSpreadInFeetPerMin(fuelMoistureContent, model);
            var slopeBearing = GetSlopeBearingInDegrees(hitInfoAtPoint);

            /* for elapsed time t, the slope vector has magnitude Ds and direction 0.
             * The wind vector has magnitude Dw in direction w from the upslope.
             * the slope vector is (Ds, 0) and the wind vector is (Dwcosw, Dwsinw). 
             * The resultant vector is then (Ds + Dwcosw, Dwsinw). 
             * The magnitude of the head fire vector is Dh in direction a.
             */
            var ds = r0 * SlopeFactor(hitInfoAtPoint, model);
            var dw = r0 * WindFactor(midflameWindSpeed.WindSpeedFeetPerMinutes, model, fuelMoistureContent);
            var w = Math.Abs(slopeBearing - windBearing);

            var x = ds + (dw * Math.Cos(DegreesToRadians(w)));
            var y = dw * Math.Sin(DegreesToRadians(w));
            var dh = Math.Pow(Math.Pow(x, 2f) + Math.Pow(y, 2f), 0.5f);

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


        /// <summary>
        /// Returns the rate of spread of fire in ft/min given no wind or slope.
        /// </summary>
        /// <param name="point">the unity point in game space</param>
        /// <param name="fuelMoistureContent"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        private double ZeroWindZeroSlopeRateOfSpreadInFeetPerMin(double fuelMoistureContent, Fuel model)
        {
            var propFluxNoWindSlope = PropagatingFluxNoWindSlope(fuelMoistureContent, model);

            var heatSink = HeatSink(fuelMoistureContent, model);

            if (propFluxNoWindSlope == 0 || heatSink == 0) { return 0; }

            return propFluxNoWindSlope / heatSink;
        }
        
        /// <summary>
        /// </summary>
        /// <param name="mf">moisture content</param>
        /// <param name="model">fuel model</param>
        /// <returns></returns>
        private static double HeatSink(double mf, Fuel model)
        {
            return model.mean_bulk_density *
                   EffectiveHeatingNumber(model) *
                   HeatOfPreignition(mf);
        }

        /// <summary>
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private static double EffectiveHeatingNumber(Fuel model)
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

        /// <summary>
        /// </summary>
        /// <param name="point"></param>
        /// <param name="fuelMoistureContent"></param>
        /// <param name="model"></param>
        /// <returns>no-wind, no-slope propagating flux</returns>
        private double PropagatingFluxNoWindSlope(double fuelMoistureContent, Fuel model)
        {
            return ReactionIntensity(fuelMoistureContent, model) * PropagatingFluxRatio(model);
        }

        /// <summary>
        /// </summary>
        /// <param name="point"></param>
        /// <param name="fuelMoistureContent"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        private static double ReactionIntensity(double fuelMoistureContent, Fuel model)
        {
            var wn = NetFuelLoad(model.oven_dry_fuel_load);
            var nM = MoistureDampingCoefficient(fuelMoistureContent, model.dead_fuel_moisture_of_extinction);
            var g = NetFuelLoadWeightingFactor(model);

            return OptimumReactionVelocity(model) *
                   (wn * g) *
                   Fuel.heat_content *
                   //nM *
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
        /// <param name="hitInfo"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        private double SlopeFactor(RaycastHit hitInfo, Fuel model)
        {
            var theta = GetSlopeInDegrees(hitInfo);

            if (model.packing_ratio == 0) { return 0.0; }
            var slopeFactor = 5.27 * Math.Pow(model.packing_ratio, -0.3) * Math.Pow(Math.Tan(DegreesToRadians(theta)), 2);

            return slopeFactor;
        }

        /// <summary>
        /// phi_w
        /// </summary>
        /// <param name="point"></param>
        /// <param name="windSpeed"></param>
        /// <param name="model"></param>
        /// <param name="fuelMoistureContent"></param>
        /// <returns></returns>
        private static double WindFactor(float windSpeed, Fuel model, double fuelMoistureContent)
        {
            if (model.relative_packing_ratio == 0f) { return 0f; }

            var c = 7.47 * Math.Exp(-0.133 * Math.Pow(model.characteristic_sav, 0.55));
            var b = 0.025256 * Math.Pow(model.characteristic_sav, 0.54);
            var e = 0.715 * Math.Exp(-3.59 * model.characteristic_sav * Math.Pow(10, -4));
            var u =
                Math.Min(
                    MaximumReliableWindSpeed(ReactionIntensity(fuelMoistureContent, model)),
                    windSpeed
                );

            return (c * Math.Pow(u, b) * Math.Pow(model.relative_packing_ratio, -e));
        }

        /// <summary>
        /// U_E
        /// </summary>
        /// <param name="point"></param>
        /// <param name="midflameWindSpeed"></param>
        /// <param name="model"></param>
        /// <param name="fuelMoistureContent"></param>
        /// <returns></returns>
        private double EffectiveMidflameWindSpeed(Vector3 point, Wind midflameWindSpeed, 
            Fuel model, double fuelMoistureContent)
        {
            var effectiveWindFactor = 
                WindFactor(midflameWindSpeed.WindSpeedFeetPerMinutes, model, fuelMoistureContent) + 
                SlopeFactor(GetHitInfo(point), model);
            
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
        
        private static double GetSlopeBearingInDegrees(RaycastHit hitInfo)
        {
            var normal = hitInfo.normal;

            var left = Vector3.Cross(normal, Vector3.down);
            var upslope = Vector3.Cross(normal, left);
            var upslopeFlat = new Vector3(upslope.x, 0, upslope.z).normalized;

            return BearingBetweenInDegrees(NORTH_VECTOR, upslopeFlat);
        }
        
        private static double BearingBetweenInDegrees(Vector3 a, Vector3 b)
        {
            var normal = Vector3.up;
            // angle in [0, 180]
            double angle = Vector3.Angle(a, b);
            double sign = Math.Sign(Vector3.Dot(normal, Vector3.Cross(a, b)));

            // angle in [-179, 180]
            var signedAngle = angle * sign;

            // angle in [0, 360]
            var bearing = (signedAngle + 360) % 360;
            return bearing;
        }
        
        private static RaycastHit GetHitInfo(Vector3 point)
        {
            var origin = new Vector3(point.x, point.y + 1000, point.z);

            if (Physics.Raycast(origin, Vector3.down, out var hitInfo, int.MaxValue))
            {
                return hitInfo;
            }
            
            throw new Exception("No Hit in Raycast.");
        }

        private static double DegreesToRadians(double deg)
        {
            return (Math.PI / 180f) * deg;
        }

        public static double FeetToMetres(double feet) => 0.3048 * feet;

    }
}