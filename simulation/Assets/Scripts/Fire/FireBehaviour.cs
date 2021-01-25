using Mapbox.Unity.Map;
using Services;
using System;
using UnityEngine;

namespace Fire
{
    public class FireBehaviour : MonoBehaviour
    {
        private bool _active = false;
        private float _minutesPassed = 0;
        private GameObject _windArrow;
        private RothermelService _rothermelService;
        private FireNode _fire;
        private const int FireNodeSizeMetres = 10;

        private void Awake()
        {
            _windArrow = Resources.Load("Prefabs/WindArrow") as GameObject;
        }

        public void Initialise(Vector3 ignitionPoint, AbstractMap map)
        {
            _rothermelService = new RothermelService(map);
            CreateWindArrow(ignitionPoint);

            if (CanBurn(ignitionPoint)) _fire = new FireNode(null, map, ignitionPoint, FireNodeSizeMetres);
            else print("Cannot start fire here.");
        }

        private void CreateWindArrow(Vector3 point)
        {
            if (point == null)
                throw new Exception("Attempted to create wind arrow before ignition point defined.");

            var weatherAtIgnition = _rothermelService.MidflameWindSpeed(point)
                .GetAwaiter().GetResult();

            var arrow = Instantiate(_windArrow, point, Quaternion.identity);
            var yAxis = new Vector3(0, 1, 0);
            arrow.transform.Rotate(yAxis, weatherAtIgnition.current.wind_deg);
            arrow.GetComponentInChildren<TextMesh>().text = $"{_rothermelService.FeetToMetres(weatherAtIgnition.current.wind_speed).ToString()}m/s";
        }

        public bool CanBurn(Vector3 point)
        {
            return _rothermelService.RateOfMaximumSpreadInFeetPerMinute(point)
                .GetAwaiter()
                .GetResult()
                .SpreadRateFeetPerMin > 0.0;
        }

        public void Increment()
        {
            _fire.UpdateTime();
        }
    }
}
