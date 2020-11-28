using Mapbox.Utils;
using Mapbox.Unity.Map;
using System.Net.Http;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;

public class FireBehaviour : MonoBehaviour
{
    public AbstractMap Map;
    public Vector3 IgnitionPoint;
    static readonly HttpClient client = new HttpClient();
    private const string ModelNumberUrl =
        "http://127.0.0.1:5000/model-number?lat={0}&lon={1}";
    private const string ModelParametersUrl =
        "http://127.0.0.1:5000/model-parameters?number={0}";
    private const string MoistureUrl =
        "http://127.0.0.1:5500/live-fuel-moisture-content?lat={0}&lon={1}";

    private void Awake()
    {
        Map = UnityEngine.Object.FindObjectOfType<AbstractMap>();
    }

    // Use this for initialization
    void Start()
    {
        var rateofSpread = RateOfSpreadNoWindSlope(IgnitionPoint);   
    }

    // Update is called once per frame
    void Update()
    {
    }


    async Task<float> RateOfSpreadNoWindSlope(Vector3 point)
    {
        Vector2d latlon = Map.WorldToGeoPosition(point);

        HttpResponseMessage response;

        response = await client.GetAsync(string.Format(ModelNumberUrl, latlon.x, latlon.y));
        response.EnsureSuccessStatusCode();
        string modelNumber = await response.Content.ReadAsStringAsync();

        response = await client.GetAsync(string.Format(ModelParametersUrl, modelNumber));
        response.EnsureSuccessStatusCode();
        string modelParameters = await response.Content.ReadAsStringAsync();
        FuelModel model = JsonUtility.FromJson<FuelModel>(modelParameters);

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

        response = await client.GetAsync(string.Format(MoistureUrl, latlon.x, latlon.y));
        response.EnsureSuccessStatusCode();
        string moisture = await response.Content.ReadAsStringAsync();

        // Environmental
        float fuelMoisture = float.Parse(moisture, CultureInfo.InvariantCulture.NumberFormat);


        float propFluxNoWindSlope =
            PropagatingFluxNoWindSlope(surfaceAreaToVolumeRatio,
                                        fuelBedDepth,
                                        ovenDryFuelLoad,
                                        particleDensity,
                                        totalMineralContent,
                                        heatContent,
                                        fuelMoisture,
                                        deadFuelMoistureOfExtinction,
                                        effectiveMineralContent);

        float heatSink = HeatSink(particleDensity,
                                    fuelMoisture,
                                    surfaceAreaToVolumeRatio);

        print($"Rate of spread with no wind or slope: {propFluxNoWindSlope / heatSink}");
        return propFluxNoWindSlope / heatSink;
    }

    #region Heat Sink
    /// <summary>
    /// </summary>
    /// <param name="pB">particle density</param>
    /// <param name="Mf">moisture content</param>
    /// <param name="sigma">surface-area-to-volume-ratio</param>
    /// <returns></returns>
    private float HeatSink(float pB, float Mf, float sigma)
    {
        return pB * EffectiveHeatingNumber(sigma) * HeatOfPreignition(Mf);
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
    private float HeatSource(float sigma, float delta, float w0, float pP,
                            float sT, float hi, float Mf, float Mx, float Se)
    {

        float propFlux = PropagatingFluxNoWindSlope(sigma, delta, w0, pP,
                                                    sT, hi, Mf, Mx, Se);

        return propFlux * (1 + SlopeFactor() + WindFactor());
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
    private float PropagatingFluxNoWindSlope(float sigma, float delta,
                                            float w0, float pP, float sT,
                                            float hi, float Mf, float Mx,
                                            float se)
    {
        return ReactionIntensity(sigma, delta, w0, pP, hi, Mf, Mx, se, sT) *
                                    PropagatingFluxRatio(sigma, delta, w0, pP);
    }

    /// <summary>
    /// </summary>
    /// <param name="sigma">surface-area-to-volume-ratio</param>
    /// <param name="delta">fuel bed depth</param>
    /// <param name="w0">oven dry fuel load</param>
    /// <param name="pP">particle density</param>
    /// <param name="hi">heat content</param>
    /// <param name="Mf">fuel moisture</param>
    /// <param name="Mx">dead fuel moisture of extinction</param>
    /// <param name="se">effective mineral content</param>
    /// <param name="sT">total mineral content</param>
    /// <returns></returns>
    private float ReactionIntensity(float sigma, float delta, float w0,
                                    float pP, float hi, float Mf,
                                    float Mx, float se, float sT)
    {
        float wn = NetFuelLoad(w0, sT);
        float nM = MoistureDampingCoefficient(Mf, Mx);
        float nS = MineralDampingCoefficient(se);

        return
            OptimumReactionVelocity(sigma, delta, w0, pP) * wn * hi * nM * nS;
    }

    /// <summary>
    /// </summary>
    /// <param name="sigma">surface-area-to-volume-ratio</param>
    /// <param name="delta">fuel bed depth</param>
    /// <param name="w0">oven-dry fuel load</param>
    /// <param name="pP">particle density</param>
    /// <returns></returns>
    private float OptimumReactionVelocity(float sigma, float delta, float w0, float pP)
    {
        float A = 133f * Mathf.Pow(sigma, -0.7913f);
        float relativePackingRatio = RelativePackingRatio(delta, w0, pP, sigma);

        return MaximumReactionVelocity(sigma) *
                Mathf.Pow(relativePackingRatio, A) *
                Mathf.Exp(A * (1f - relativePackingRatio));
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
    /// <param name="sigma">surface-area-to-volume-ratio</param>
    /// <param name="delta">fuel bed depth</param>
    /// <param name="w0">oven-dry fuel load</param>
    /// <param name="pP">particle density</param>
    /// <returns></returns>
    private float PropagatingFluxRatio(float sigma, float delta, float w0, float pP)
    {
        float beta = MeanPackingRatio(delta, w0, pP);

        return Mathf.Pow(192f + (0.2595f * sigma), -1f) *
            Mathf.Exp((0.792f + (0.681f * Mathf.Pow(sigma, 0.5f))) * (beta + 0.1f));
    }

    /// <summary>
    /// </summary>
    /// <param name="w0">oven dry fuel load</param>
    /// <param name="sT">total mineral content</param>
    /// <returns></returns>
    private float NetFuelLoad(float w0, float sT)
    {
        return w0 * (1 - sT);
    }

    /// <summary>
    /// </summary>
    /// <param name="Se">effective mineral content</param>
    /// <returns></returns>
    private float MineralDampingCoefficient(float Se)
    {
        float coefficient = 0.174f * Mathf.Pow(Se, -0.19f);
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
    /// <param name="delta">fuel bed depth</param>
    /// <param name="w0">oven-dry fuel load</param>
    /// <param name="pP">particle density</param>
    /// <returns></returns>
    private float MeanPackingRatio(float delta, float w0, float pP)
    {
        return (1 / delta) * (w0 / pP);
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
    private float RelativePackingRatio(float delta, float w0, float pP, float sigma)
    {
        return MeanPackingRatio(delta, w0, pP) / OptimumPackingRatio(sigma);
    }
    #endregion

    private float SlopeFactor()
    {
        return 0f;
    }

    private float WindFactor()
    {
        return 0f;
    }
}
