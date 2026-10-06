using System;
using UnityEngine;
using UnityEngine.InputSystem;

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

        public Station CurrentStation => Map != null ? Map.stations[Session.StationIndex] : null;
        public bool AtStationEnd => CurrentStation != null && !Traveler.IsMoving &&
            Mathf.Abs(Map.RouteDistanceAtProgress(Traveler.Progress01) - CurrentStation.TileCenter(CurrentStation.tileCount - 1)) < .001f;
        public bool CanDepart => !IsPreviewing && AtStationEnd && Session.Ready;

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
            var mouse = Mouse.current;
            if (mouse != null && Application.isFocused && mouse.leftButton.wasPressedThisFrame &&
                Session.Phase == JourneyPhase.AtStation && !IsPreviewing && !UI.IsModalOpen &&
                Placement.SelectedMaterialId == null && !PointerUtility.OverUI(mouse.position.ReadValue()) &&
                TryPointerDistance(mouse.position.ReadValue(), out float distance))
            {
                var station = CurrentStation;
                if (distance >= station.startDistance && distance < station.startDistance + station.length)
                    MoveToTile(Mathf.FloorToInt((distance - station.startDistance) / station.TileWidth));
            }
        }
        public MaterialDefinition Material(string id) => Array.Find(Config.materials, entry => entry.id == id);

        public void StartJourney(JourneyMode mode)
        {
            if (Session.Phase != JourneyPhase.Start || Map != null) return;
            try
            {
                Map = MapProvider.CreateMap(WorldRoot);
                if (!Map.Validate(out string error)) throw new InvalidOperationException(error);
                Session.Begin(Map.stations.Length, Config.WaitSeconds(mode), Config.materials, false);
                generation++; Mode = mode;
                Traveler.gameObject.SetActive(true);
                Traveler.PlaceOnRoute(Map);
                Camera.ShowStation(Map.stations[0]);
                lastTime = Time.realtimeSinceStartupAsDouble;
                Placement.ClearSelection();
                Placement.OnTravelerArrived();
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
            if (!CanDepart || !Session.TryDepart()) return;
            UI.CloseExitPrompt();
            UI.ClosePlacementPrompt(false);
            if (Session.StationIndex + 1 < Map.stations.Length) Camera.BeginStationTransition();
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
                    Placement.OnTravelerArrived();
                    UI.Notify("已抵达第 " + (Session.StationIndex + 1) + " 站");
                }
                Placement.RefreshHighlights();
            };
            int next = Session.ApproachingFirstStation ? 0 : Session.StationIndex + 1;
            float target = next < Map.stations.Length ? Map.stations[next].stopProgress : 1;
            Traveler.MoveToProgress(target, Config.TravelSeconds(next, Map.stations.Length), onArrived);
        }
        public bool TryPointerDistance(Vector2 pointer, out float distance)
        {
            distance = 0;
            if (Map == null) return false;
            var plane = new Plane(Map.transform.up, Map.transform.TransformPoint(new Vector3(0, Map.sourceSettings.ringThickness * .5f, 0)));
            var ray = Camera.WorldCamera.ScreenPointToRay(pointer);
            if (!plane.Raycast(ray, out float enter)) return false;
            var local = Map.transform.InverseTransformPoint(ray.GetPoint(enter));
            distance = local.x + Map.RouteLength * .5f;
            return true;
        }
        public bool MoveToTile(int tile)
        {
            if (Map == null || IsPreviewing || UI.IsModalOpen || Session.Phase != JourneyPhase.AtStation ||
                tile < 0 || tile >= CurrentStation.tileCount) return false;
            float target = Map.ProgressAtDistance(CurrentStation.TileCenter(tile));
            float tiles = Mathf.Abs(Map.RouteDistanceAtProgress(target) - Map.RouteDistanceAtProgress(Traveler.Progress01)) / Map.TileWidth;
            Traveler.CancelMovement();
            Camera.ResumeFollow();
            int currentGeneration = generation;
            Traveler.MoveToProgress(target, tiles / Mathf.Max(.1f, Config.walkTilesPerSecond), () =>
            {
                if (generation == currentGeneration) Placement.OnTravelerArrived();
            });
            return true;
        }
        public void ReturnToStart()
        {
            generation++;
            UI.CloseExitPrompt();
            UI.ClosePlacementPrompt(false);
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
            UI.CloseExitPrompt();
            UI.ClosePlacementPrompt(false);
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
            Camera.RestoreJourneyView(); Placement.RefreshHighlights();
        }
        void OnDestroy() { if (Traveler != null) Traveler.CancelMovement(); }
    }
}

