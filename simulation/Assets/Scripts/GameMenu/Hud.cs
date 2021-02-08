using System;
using System.Linq;
using External;
using Mapbox.Utils;
using Player;
using UnityEngine;
using UnityEngine.UI;
using static Constants.StringConstants;

namespace GameMenu
{
    public class Hud : MonoBehaviour
    {
        public Button ignite;
        public Button pause;
        public Button settings;
        public Button next30Min;
        public Button createControlLine;
        public GameObject holding;

        private bool _pauseClicked;
        
        public bool IgniteClicked { get; set; }
        public PlayerController playerController;
        
        public void ToggleIgniting()
        {
            IgniteClicked = !IgniteClicked;
            ignite.GetComponent<Image>().color =
                MenuConstants.GetIgnitingButtonColor(IgniteClicked);

        }

        public void TogglePaused()
        {
            _pauseClicked = !_pauseClicked;
            
            pause.GetComponent<Image>().color =
                MenuConstants.GetPausedButtonColor(_pauseClicked);
            pause.GetComponentInChildren<Text>().text =
                MenuConstants.GetPausedButtonText(_pauseClicked);

            playerController.PauseAllFires(_pauseClicked);
        }

        public void SetInteractable(bool interactable)
        {
            ignite.interactable = interactable;
            pause.interactable = interactable;
        }

        public void Next30Min()
        {
            playerController.Increment(30);
        }

        public void SpawnAndHoldControlLine()
        {
            var controlLine = Resources.Load(ControlLinePrefab) as GameObject;
            holding = Instantiate(controlLine);
        }

        public void DropControlLine()
        {
            const int aboveTerrain = 10000;
            var controlLineBounds = holding.GetComponent<Renderer>().bounds;
            
            var minWorld = controlLineBounds.min;
            var maxWorld = controlLineBounds.max;
            
            Vector2d minGeo;
            Vector2d maxGeo;

            // raycast down from min
            if (Physics.Raycast(new Vector3(minWorld.x, aboveTerrain, minWorld.z), Vector3.down, out var hitInfo, Mathf.Infinity))
            {
                minGeo = playerController.map.WorldToGeoPosition(hitInfo.point);
            }
            else throw new Exception("Can't drop here.");
            
            // raycast down from max
            if (Physics.Raycast(new Vector3(maxWorld.x, aboveTerrain, maxWorld.z), Vector3.down, out hitInfo, Mathf.Infinity))
            {
                maxGeo = playerController.map.WorldToGeoPosition(hitInfo.point);
            }
            else throw new Exception("Can't drop here.");
            
            FuelModelProvider.PutControlLine(minGeo, maxGeo);
            
            
            holding = null;
        }

    }
}
