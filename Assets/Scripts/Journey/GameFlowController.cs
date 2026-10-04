using System;
using UnityEngine;

namespace Hiking.Journey
{
    public class GameFlowController : MonoBehaviour
    {
        public JourneyConfig Config;
        public MapProvider MapProvider;
        public TravelerController Traveler;
        public PlacementController Placement;
        public CameraController Camera;
        public GameUIController UI;
        public Transform WorldRoot;
        public JourneySession Session { get; } = new JourneySession();
        public JourneyMap Map { get; private set; }
        public JourneyMode Mode { get; private set; }
        double lastTime;
        int generation;
        public bool IsPreviewing { get; private set; }

        void Start()
        {
            Application.runInBackground = true;
            Placement.Initialize(this); Camera.Initialize(this); UI.Initialize(this);
            ReturnToStart();
        }
        void Update()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            if (!IsPreviewing) Session.Tick(now - lastTime);
            lastTime = now;
        }
        public MaterialDefinition Material(string id) => Array.Find(Config.materials, entry => entry.id == id);

        public void StartJourney(JourneyMode mode)
        {
            if (Session.Phase != JourneyPhase.Start || Map != null) return;
            try
            {
                Map = MapProvider.CreateMap(WorldRoot);
                if (!Map.Validate(out string error)) throw new InvalidOperationException(error);
                Session.Begin(Map.stations.Length, Config.WaitSeconds(mode), Config.materials, true);
                generation++; Mode = mode;
                Traveler.gameObject.SetActive(true);
                Traveler.PlaceOnRoute(Map);
                Camera.ShowStation(Map.stations[0]);
                lastTime = Time.realtimeSinceStartupAsDouble;
                Placement.ClearSelection();
            }
            catch (Exception error)
            {
                ReturnToStart();
                UI.Notify("无法开始旅途：" + error.Message);
                Debug.LogException(error);
            }
        }
        public void Depart()
        {
            if (IsPreviewing || !Session.TryDepart()) return;
            Placement.ClearSelection();
            int currentGeneration = generation;
            Action onArrived = () =>
            {
                if (generation != currentGeneration || !Session.Arrive()) return;
                if (Session.Phase == JourneyPhase.Revealing)
                    Camera.RevealPanorama(Map.panoramaBounds, () =>
                    {
                        if (generation == currentGeneration) Session.FinishReveal();
                    });
                else
                {
                    // Arrival starts a new recharge period, excluding the preceding movement frame.
                    lastTime = Time.realtimeSinceStartupAsDouble;
                    Camera.ShowStation(Map.stations[Session.StationIndex]);
                    UI.Notify("已抵达第 " + (Session.StationIndex + 1) + " 站");
                }
                Placement.RefreshHighlights();
            };
            int next = Session.ApproachingFirstStation ? 0 : Session.StationIndex + 1;
            float target = next < Map.stations.Length ? Map.stations[next].stopProgress : 1;
            Traveler.MoveToProgress(target, Config.TravelSeconds(next, Map.stations.Length), onArrived);
        }
        public void ReturnToStart()
        {
            generation++;
            IsPreviewing = false; Traveler.Paused = false;
            Traveler.CancelMovement(); Camera.ResetCamera(); Placement.ClearSelection();
            Traveler.transform.SetParent(null, true);
            Traveler.gameObject.SetActive(false);
            if (Map != null) { Map.gameObject.SetActive(false); Destroy(Map.gameObject); Map = null; }
            Session.Reset();
            lastTime = Time.realtimeSinceStartupAsDouble;
            UI.Notify("");
        }
        public void PreviewPanorama()
        {
            if (Map == null || IsPreviewing || Session.Phase == JourneyPhase.Revealing || Session.Phase == JourneyPhase.Panorama) return;
            IsPreviewing = true; Traveler.Paused = true;
            Placement.ClearSelection();
            Camera.RevealPanorama(Map.panoramaBounds, () => { });
        }
        public void ExitPreview()
        {
            if (!IsPreviewing || Map == null) return;
            Camera.ResetCamera(); Map.ShowJourney(); Traveler.RefreshPosition();
            IsPreviewing = false; Traveler.Paused = false;
            lastTime = Time.realtimeSinceStartupAsDouble;
            Camera.ShowStation(Map.stations[Session.StationIndex]); Placement.RefreshHighlights();
        }
        void OnDestroy() { if (Traveler != null) Traveler.CancelMovement(); }
    }
}

