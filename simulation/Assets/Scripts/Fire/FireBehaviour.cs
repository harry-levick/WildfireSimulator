using Mapbox.Utils;
using Mapbox.Unity.Map;
using System;
using System.Net.Http;
using System.Globalization;
using System.Threading.Tasks;
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


    private void Awake()
    {
        Map = UnityEngine.Object.FindObjectOfType<AbstractMap>();
    }

    // Use this for initialization
    void Start()
    {
        var rateofSpread = RateOfSpreadNoWindSlope(IgnitionPoint);
        RaycastHit hitInfo = GetHitInfo(IgnitionPoint);
        var slope = GetSlopeInDegrees(hitInfo);
        var slopeBearing = GetSlopeBearingInDegrees(hitInfo);
    }

    // Update is called once per frame
    void Update()
    {
    }

    async Task<float> RateOfSpreadSameWindSlopeDirection(Vector3 point)
    {
        float r0 = await RateOfSpreadNoWindSlope(point); // zero-wind, zero-slope rate of spread
        float windFactor;
        float slopeFactor;


        return 0.0f;
    }


    async Task<float> RateOfSpreadNoWindSlope(Vector3 point)
    {
        FuelModel model = await FuelModelParameters(point);
        float fuelMoisture = await FuelMoistureContent(point);

        // Fuel Particle
        int heatContent = FuelModel.heat_content;
        float totalMineralContent = FuelModel.total_mineral_content;
        float effectiveMineralContent = FuelModel.effective_mineral_content;
        float particleDensity = FuelModel.particle_density;

        // Fuel Array
        float surfaceAreaToVolumeRatio = model.characteristic_sav;
        float ovenDryFuelLoad = model.oven_dry_fuel_load;
        float fuelBedDepth = model.fuel_bed_depth;
        float deadFuelMoistureOfExtinction = model.dead_fuel_moisture_of_extinction;

        float propFluxNoWindSlope =
            PropagatingFluxNoWindSlope(fuelMoisture, model);

        float heatSink = HeatSink(fuelMoisture, model);

        print($"Rate of spread with no wind or slope: {propFluxNoWindSlope / heatSink}");
        return propFluxNoWindSlope / heatSink;
    }

    #region Heat Sink
    /// <summary>
    /// </summary>
    /// <param name="Mf">moisture content</param>
    /// <param name="model">fuel model</param>
    /// <returns></returns>
    private float HeatSink(float Mf, FuelModel model)
    {
        return FuelModel.particle_density *
                EffectiveHeatingNumber(model.characteristic_sav) *
                HeatOfPreignition(Mf);
    }

    /// <summary>
    /// </summary>
    /// <param name="sigma">surface-area-to-volume-ratio</param>
    /// <returns></returns>
    private float EffectiveHeatingNumber(float sigma)
    {
        return Mathf.Exp(- 138 / sigma);
    }

    /// <summary>
    /// </summary>
    /// <param name="Mf">moisture content</param>
    /// <returns></returns>
    private float HeatOfPreignition(float Mf)
    {
        return 250 + (1116 * Mf);
    }
    #endregion

    #region Heat Source
    /// <summary>
    /// </summary>
    /// <param name="sigma"></param>
    /// <param name="delta"></param>
    /// <param name="w0"></param>
    /// <param name="pP"></param>
    /// <param name="sT"></param>
    /// <param name="hi"></param>
    /// <param name="Mf"></param>
    /// <param name="Mx"></param>
    /// <param name="Se"></param>
    /// <returns></returns>
    private float HeatSource(FuelModel model, float Mf, float theta)
    {

        float propFlux = PropagatingFluxNoWindSlope(Mf, model);

        float wind = 
        return propFlux * (1 + SlopeFactor(theta, model) + WindFactor(wind, model));
    }

    /// <summary>
    /// </summary>
    /// <param name="sigma">surface-area-to-volume-ratio</param>
    /// <param name="delta">fuel bed depth</param>
    /// <param name="w0">oven-dry fuel load</param>
    /// <param name="pP">particle density</param>
    /// <param name="sT">total mineral content</param>
    /// <param name="hi">heat content</param>
    /// <param name="Mf">fuel moisture</param>
    /// <param name="Mx">dead fuel moisture of extinction</param>
    /// <param name="se">effective mineral content</param>
    /// <returns>no-wind, no-slope propagating flux</returns>
    private float PropagatingFluxNoWindSlope(float Mf, FuelModel model)
    {
        return ReactionIntensity(Mf, model) * PropagatingFluxRatio(model);
    }

    /// <summary>
    /// </summary>
    /// <param name="Mf">fuel moisture</param>
    /// <param name="model">fuel model</param>
    /// <returns></returns>
    private float ReactionIntensity(float Mf, FuelModel model)
    {
        float wn = NetFuelLoad(model.oven_dry_fuel_load);
        float nM = MoistureDampingCoefficient(Mf, model.dead_fuel_moisture_of_extinction);

        return OptimumReactionVelocity(model) *
                wn *
                FuelModel.heat_content *
                nM *
                MineralDampingCoefficient();
    }

    /// <summary>
    /// </summary>
    /// <param name="model">fuel model</param>
    /// <returns></returns>
    private float OptimumReactionVelocity(FuelModel model)
    {
        float A = 133f * Mathf.Pow(model.characteristic_sav, -0.7913f);

        return MaximumReactionVelocity(model.characteristic_sav) *
                Mathf.Pow(model.relative_packing_ratio, A) *
                Mathf.Exp(A * (1f - model.relative_packing_ratio));
    }

    /// <summary>
    /// </summary>
    /// <param name="sigma">surface-area-to-volume-ratio</param>
    /// <returns></returns>
    private float MaximumReactionVelocity(float sigma)
    {
        return Mathf.Pow(sigma, 1.5f) *
                Mathf.Pow(495f + (0.0594f * Mathf.Pow(sigma, 1.5f)), -1f);
    }

    /// <summary>
    /// </summary>
    /// <param name="model">fuel model</param>
    /// <returns></returns>
    private float PropagatingFluxRatio(FuelModel model)
    {
        float beta = MeanPackingRatio(model);

        return Mathf.Pow(192f + (0.2595f * model.characteristic_sav), -1f) *
            Mathf.Exp((0.792f + (0.681f * Mathf.Pow(model.characteristic_sav, 0.5f))) * (beta + 0.1f));
    }

    /// <summary>
    /// </summary>
    /// <param name="w0">oven dry fuel load</param>
    /// <param name="sT">total mineral content</param>
    /// <returns></returns>
    private float NetFuelLoad(float w0)
    {
        return w0 * (1 - FuelModel.total_mineral_content);
    }

    /// <summary>
    /// </summary>
    /// <param name="Se">effective mineral content</param>
    /// <returns></returns>
    private float MineralDampingCoefficient()
    {
        float coefficient = 0.174f * Mathf.Pow(FuelModel.effective_mineral_content, -0.19f);
        return Mathf.Min(coefficient, 1f); // (max = 1)
    }

    /// <summary>
    /// </summary>
    /// <param name="Mf">moisture content</param>
    /// <param name="Mx">dead fuel moisture of extinction</param>
    /// <returns></returns>
    private float MoistureDampingCoefficient(float Mf, float Mx)
    {
        float rM = Mathf.Min((Mf / Mx), 1); // (max = 1)

        return (1f - (2.59f * rM)) +
                (5.11f * Mathf.Pow(rM, 2f)) -
                (3.52f * Mathf.Pow(rM, 3f));
    }

    /// <summary>
    /// </summary>
    /// <param name="model">fuel model</param>
    /// <returns></returns>
    private float MeanPackingRatio(FuelModel model)
    {
        return (1 / model.fuel_bed_depth) * (model.oven_dry_fuel_load / FuelModel.particle_density);
    }

    /// <summary>
    /// </summary>
    /// <param name="sigma">surface-area-to-volume-ratio</param>
    /// <returns></returns>
    private float OptimumPackingRatio(float sigma)
    {
        return 3.348f * Mathf.Pow(sigma, -0.8189f);
    }

    /// <summary>
    /// </summary>
    /// <param name="delta">fuel bed depth</param>
    /// <param name="w0"><oven-dry fuel load/param>
    /// <param name="pP">particle density</param>
    /// <param name="sigma">surface-area-to-volume-ratio</param>
    /// <returns></returns>
    private float RelativePackingRatio(FuelModel model)
    {
        return MeanPackingRatio(model) / OptimumPackingRatio(model.characteristic_sav);
    }
    #endregion

    #region Environmental

    /// <summary>
    /// </summary>
    /// <param name="theta">slope angle</param>
    /// <param name="beta">packing ratio</param>
    /// <returns></returns>
    private float SlopeFactor(float theta, FuelModel model)
    {
        return 5.27f * Mathf.Pow(model.packing_ratio, -0.3f) * Mathf.Pow(Mathf.Tan(theta), 2f);
    }

    /// <summary>
    /// </summary>
    /// <param name="wind">midflame wind speed</param>
    /// <param name="model"></param>
    /// <returns></returns>
    private float WindFactor(float wind, FuelModel model)
    {
        float C = 7.47f * Mathf.Exp(-0.133f * Mathf.Pow(model.characteristic_sav, 0.55f));
        float B = 0.025256f * Mathf.Pow(model.characteristic_sav, 0.54f);
        float E = 0.715f * Mathf.Exp(-3.59f * model.characteristic_sav * Mathf.Pow(10f, -4f));

        return (float)(C * Mathf.Pow(wind, B) * Math.Pow(model.relative_packing_ratio, -E));
    }

    float GetSlopeInDegrees(RaycastHit hitInfo)
    {
        Vector3 normal = hitInfo.normal;
        return Vector3.Angle(normal, Vector3.up);
    }

    float GetSlopeBearingInDegrees(RaycastHit hitInfo)
    {
        Vector3 normal = hitInfo.normal;

        Vector3 left = Vector3.Cross(normal, Vector3.down);
        Vector3 upslope = Vector3.Cross(normal, left);
        Vector3 upslopeFlat = new Vector3(upslope.x, 0, upslope.z).normalized;

        return BearingBetweenInDegrees(North, upslopeFlat);
    }

    float BearingBetweenInDegrees(Vector3 a, Vector3 b)
    {
        Vector3 normal = Vector3.up;
        // angle in [0, 180]
        float angle = Vector3.Angle(a, b);
        float sign = Mathf.Sign(Vector3.Dot(normal, Vector3.Cross(a, b)));

        // angle in [-179, 180]
        float signedAngle = angle * sign;

        // angle in [0, 360]
        float bearing = (signedAngle + 360) % 360;
        return bearing;
    }

    RaycastHit GetHitInfo(Vector3 point)
    {
        Vector3 origin = new Vector3(point.x, point.y + 100, point.z);

        RaycastHit hitInfo;
        if (Physics.Raycast(origin, Vector3.down, out hitInfo, Mathf.Infinity))
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

    #endregion

    #region api calls
    async Task<int> FuelModelNumber(Vector3 point)
    {
        HttpResponseMessage response;

        Vector2d latlon = Map.WorldToGeoPosition(point);

        response = await client.GetAsync(string.Format(ModelNumberUrl, latlon.x, latlon.y));
        response.EnsureSuccessStatusCode();
        string modelNumber = await response.Content.ReadAsStringAsync();
        return Int32.Parse(modelNumber);
    }

    async Task<FuelModel> FuelModelParameters(Vector3 point)
    {
        HttpResponseMessage response;
        Vector2d latlon = Map.WorldToGeoPosition(point);

        int modelNumber = await FuelModelNumber(point);
        response = await client.GetAsync(string.Format(ModelParametersUrl, modelNumber));
        response.EnsureSuccessStatusCode();

        return
            JsonUtility.FromJson<FuelModel>(
                        await response.Content.ReadAsStringAsync()
            );
    }

    async Task<float> FuelMoistureContent(Vector3 point)
    {
        HttpResponseMessage response;
        Vector2d latlon = Map.WorldToGeoPosition(point);

        response = await client.GetAsync(string.Format(MoistureUrl, latlon.x, latlon.y));
        response.EnsureSuccessStatusCode();
        return
            float.Parse(
                await response.Content.ReadAsStringAsync(),
                CultureInfo.InvariantCulture.NumberFormat
            );
    }

    async Task<float> MidflameWindSpeed(Vector3 point)
    {
        HttpResponseMessage response;
        Vector2d latlon = Map.WorldToGeoPosition(point);

        response = await client.GetAsync(string.Format(WindSpeedUrl, latlon.x, latlon.y));
        response.EnsureSuccessStatusCode();
        return
            float.Parse(
                await response.Content.ReadAsStringAsync(),
                CultureInfo.InvariantCulture.NumberFormat
            );
    }
    #endregion
}
