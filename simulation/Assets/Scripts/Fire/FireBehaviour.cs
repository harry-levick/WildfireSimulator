using System;
using System.Collections;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using Model;
using Services;
using UnityEngine;
using UnityEngine.UI;

namespace Fire
{
    public class FireBehaviour : MonoBehaviour
    {
        private readonly Vector3 _north = new Vector3(0, 0, 1);
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

        private AbstractMap _map;
        private Vector3 _ignitionPoint;
        private double _lengthWidthRatio;
        private double _eccentricity;
        private double _headingFireRateOfSpreadMetresPerMin;
        private double _headingFireBearing;
        private double _backingFireRateOfSpread;
        private double _backingFireBearing;
        private GameObject _windArrow;

        private Hashtable _directions = new Hashtable();
        private RothermelService _rothermelService;

        private void Awake()
        {
            _windArrow = Resources.Load("Prefabs/WindArrow") as GameObject;
        }

        public void Initialise(Vector3 ignitionPoint, AbstractMap map)
        {
            _ignitionPoint = ignitionPoint;
            _map = map;
            _rothermelService = new RothermelService(_map);
            CreateWindArrow();
        }

        private void CreateWindArrow()
        {
            if (_ignitionPoint == null)
                throw new Exception("Attempted to create wind arrow before ignition point defined.");

            var weatherAtIgnition = _rothermelService.MidflameWindSpeed(_ignitionPoint)
                .GetAwaiter().GetResult();

            var arrow = Instantiate(_windArrow, _ignitionPoint, Quaternion.identity);
            var yAxis = new Vector3(0, 1, 0);
            arrow.transform.Rotate(yAxis, weatherAtIgnition.current.wind_deg);
            arrow.GetComponentInChildren<TextMesh>().text = $"{_rothermelService.FeetToMetres(weatherAtIgnition.current.wind_speed).ToString()}m/s";
        }

        public bool CanBurn()
        {
            return _rothermelService.RateOfMaximumSpreadInFeetPerMinute(_ignitionPoint)
                .GetAwaiter()
                .GetResult()
                .SpreadRateFeetPerMin > 0.0;
        }

        public void Ignite()
        {
            var maxSpread = _rothermelService.RateOfMaximumSpreadInFeetPerMinute(_ignitionPoint).GetAwaiter().GetResult();
            _headingFireRateOfSpreadMetresPerMin = _rothermelService.FeetToMetres(maxSpread.SpreadRateFeetPerMin);
            _headingFireBearing = maxSpread.SpreadBearing;

            if (_headingFireRateOfSpreadMetresPerMin == 0.0) return; // can't start a fire here

            _lengthWidthRatio = 1 + (0.25 * _rothermelService.EffectiveMidflameWindSpeed(_ignitionPoint).GetAwaiter().GetResult());
            _eccentricity = Math.Pow((Math.Pow(_lengthWidthRatio, 2.0) - 1.0), 0.5) / _lengthWidthRatio;

            _backingFireRateOfSpread = _headingFireRateOfSpreadMetresPerMin * ((1 - _eccentricity) / (1 + _eccentricity));
            _backingFireBearing = _rothermelService.ReverseBearing(_headingFireBearing);

            for (var angle = 0; angle <= 170; angle += 10)
            {
                if (angle == 0) { _directions.Add(angle, _headingFireRateOfSpreadMetresPerMin); }
                else
                {
                    var spreadRate = _headingFireRateOfSpreadMetresPerMin * ((1 - _eccentricity) / (1 - (_eccentricity * Math.Cos(_rothermelService.DegreesToRadians(angle)))));
                    _directions.Add(angle, spreadRate);
                }
            }
            
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
                clockwiseAngle = _rothermelService.DegreesToRadians(clockwiseAngle);
                if (anticlockwiseAngle < 0.0) { anticlockwiseAngle += 360.0; }
                anticlockwiseAngle = _rothermelService.DegreesToRadians(anticlockwiseAngle);

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
                                (float) (_ignitionPoint.x + (db * Math.Sin(_rothermelService.DegreesToRadians(_backingFireBearing)))),
                                (float) (_ignitionPoint.z + (db * Math.Cos(_rothermelService.DegreesToRadians(_backingFireBearing))))
                                );

            Debug.DrawRay(new Vector3(backingPos.x, _ignitionPoint.y, backingPos.y), Vector3.up * 100, Color.blue, 1000f);

            // TODO: add method of changing time in the Hud Menu
            const float timeIncrementInHours = 0.5f;
            _hoursPassed += timeIncrementInHours * 60f;
        }
    }
}
