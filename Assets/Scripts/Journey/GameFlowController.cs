using System;
using UnityEngine;

namespace Hiking.Journey
{
    public enum JourneyActivity { None, Walking, Resting, AwaitingPlacement, BetweenStations, Complete }
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
        public JourneyActivity Activity { get; private set; }
        public int CurrentTileIndex { get; private set; }
        public float RestRemainingSeconds { get; private set; }
        double lastTime;
        int generation;
        public bool IsPreviewing { get; private set; }
        public Station CurrentStation => Map != null ? Map.stations[Session.StationIndex] : null;
        void Start()
        {
            Application.runInBackground = true;
            Placement.Initialize(this); Camera.Initialize(this); UI.Initialize(this);
            ReturnToStart();
        }
        void Update()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            float delta = (float)(now - lastTime); lastTime = now;
            if (IsPreviewing) return;
            Session.Tick(delta);
            if (Activity != JourneyActivity.Resting || UI.IsModalOpen) return;
            RestRemainingSeconds = Mathf.Max(0, RestRemainingSeconds - delta);
            if (RestRemainingSeconds <= 0) Advance();
        }
        public MaterialDefinition Material(string id) => Array.Find(Config.materials, entry => entry.id == id);
        public void StartJourney(JourneyMode mode)
        {
            if (Session.Phase != JourneyPhase.Start || Map != null) return;
            try
            {
                Map = MapProvider.CreateMap(WorldRoot);
                if (!Map.Validate(out string error)) throw new InvalidOperationException(error);
                Session.Begin(Map.stations.Length, .1, Config.materials);
                generation++; Mode = mode; CurrentTileIndex = 0;
                Traveler.gameObject.SetActive(true); Traveler.PlaceOnRoute(Map);
                Camera.ShowStation(CurrentStation);
                lastTime = Time.realtimeSinceStartupAsDouble;
                Placement.ClearSelection();
                // Spawn at the first tile and start the first timed step immediately.
                Advance();
            }
            catch (Exception error)
            {
                ReturnToStart(); UI.Notify("无法开始旅途：" + error.Message); Debug.LogException(error);
            }
        }
        void Advance()
        {
            if (Map == null || IsPreviewing || Traveler.IsMoving || UI.IsModalOpen) return;
            if (CurrentTileIndex == CurrentStation.tileCount - 1) { Depart(); return; }
            Activity = JourneyActivity.Walking;
            int nextTile = CurrentTileIndex + 1, currentGeneration = generation;
            Traveler.MoveToProgress(Map.ProgressAtDistance(CurrentStation.TileCenter(nextTile)), Mathf.Max(.1f, Config.tileMoveSeconds), () =>
            {
                if (generation != currentGeneration) return;
                CurrentTileIndex = nextTile;
                if (Placement.CurrentSlot != null)
                {
                    Activity = JourneyActivity.AwaitingPlacement;
                    Placement.OnTravelerArrived();
                }
                else BeginRest();
            });
        }
        void BeginRest()
        {
            Activity = JourneyActivity.Resting;
            RestRemainingSeconds = Mathf.Max(0, Config.tileRestSeconds);
            lastTime = Time.realtimeSinceStartupAsDouble;
        }
        public void CompletePlacementVisit()
        {
            if (Activity != JourneyActivity.AwaitingPlacement || IsPreviewing) return;
            UI.ClosePlacementPrompt(false); BeginRest();
        }
        void Depart()
        {
            if (!Session.TryDepart(true)) return;
            Placement.ClearSelection();
            int currentGeneration = generation;
            if (Session.StationIndex == Map.stations.Length - 1)
            {
                Session.Arrive(); Activity = JourneyActivity.Complete;
                Camera.RevealPanorama(Map.panoramaBounds, () =>
                {
                    if (generation == currentGeneration) Session.FinishReveal();
                });
                return;
            }
            Activity = JourneyActivity.BetweenStations; Camera.BeginStationTransition();
            Traveler.MoveToProgress(Map.stations[Session.StationIndex + 1].stopProgress, Mathf.Max(.1f, Config.tileMoveSeconds), () =>
            {
                if (generation != currentGeneration || !Session.Arrive()) return;
                CurrentTileIndex = 0; Camera.ShowStation(CurrentStation); BeginRest();
                UI.Notify("已抵达第 " + (Session.StationIndex + 1) + " 站");
            });
        }
        public void ReturnToStart()
        {
            generation++; UI.CloseExitPrompt(); UI.ClosePlacementPrompt(false);
            IsPreviewing = false; Traveler.Paused = false;
            Activity = JourneyActivity.None; RestRemainingSeconds = 0; CurrentTileIndex = 0;
            Traveler.CancelMovement(); Camera.ResetCamera(); Placement.ClearSelection();
            Traveler.transform.SetParent(null, true); Traveler.gameObject.SetActive(false);
            if (Map != null) { Map.gameObject.SetActive(false); Destroy(Map.gameObject); Map = null; }
            Session.Reset(); lastTime = Time.realtimeSinceStartupAsDouble; UI.Notify("");
        }
        public void PreviewPanorama()
        {
            if (Map == null || IsPreviewing || UI.IsModalOpen || Session.Phase == JourneyPhase.Revealing || Session.Phase == JourneyPhase.Panorama) return;
            IsPreviewing = true; Traveler.Paused = true;
            Camera.RevealPanorama(Map.panoramaBounds, () => { });
        }
        public void ExitPreview()
        {
            if (!IsPreviewing || Map == null) return;
            Camera.ResetCamera(); Map.ShowJourney(); Traveler.RefreshPosition();
            IsPreviewing = false; Traveler.Paused = false; lastTime = Time.realtimeSinceStartupAsDouble;
            Camera.RestoreJourneyView(); Placement.RefreshHighlights();
        }
        void OnDestroy() { if (Traveler != null) Traveler.CancelMovement(); }
    }
}
