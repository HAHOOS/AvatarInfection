using System.Collections;

using AvatarInfection.Helper;
using AvatarInfection.Settings;
using AvatarInfection.Utilities;

using Il2CppSLZ.Bonelab;

using LabFusion.Player;
using LabFusion.SDK.Gamemodes;
using LabFusion.Utilities;

using MelonLoader;

using UnityEngine;

using static UnityEngine.GraphicsBuffer;

namespace AvatarInfection.Managers
{
    internal static class VisionManager
    {
        internal static bool HideVision { get; set; }

        private static TeamManager TeamManager => Infection.Instance.TeamManager;

        private static InfectionSettings Config => Infection.Instance.Config;

        private static Team Infected => Infection.Instance.Infected.Team;

        internal static void Setup()
            => MultiplayerHooking.OnUpdate += OnUpdate;

        internal static void Destroy()
            => MultiplayerHooking.OnUpdate -= OnUpdate;

        private static void OnUpdate()
        {
            if (HideVision && !LocalVision.Blind)
                LocalVision.Blind = true;
        }

        internal static void HideVisionAndReveal(float delaySeconds = 0)
            => MelonCoroutines.Start(Internal_HideVisionAndReveal(delaySeconds));

        private static IEnumerator Internal_HideVisionAndReveal(float delaySeconds = 0)
        {
            try
            {
                if (IsCountdownActive() && IsAllowed())
                {
                    if (delaySeconds > 0)
                        yield return new WaitForSeconds(delaySeconds);

                    InfectedVisionEffect(true);

                    var target = Infection.Instance.CountdownValue.GetValue();

                    const float fadeLength = 1f;

                    float elapsed = 0f;
                    float totalElapsed = 0f;

                    int seconds = 0;

                    bool secondPassed = true;

                    while (seconds < target)
                    {
                        if (!Infection.Instance.IsStarted || (Infection.Instance.InfectedLooking.GetValue() && target > 5))
                            break;

                        if (TeamManager.GetLocalTeam() == Infected)
                        {
                            // Calculate fade-in
                            float fadeStart = Mathf.Max(target - fadeLength, 0f);
                            float fadeProgress = Mathf.Max(totalElapsed - fadeStart, 0f) / fadeLength;

                            LocalVision.BlindColor = Color.Lerp(Color.black, Color.clear, fadeProgress);
                        }

                        HandleTime(target, ref elapsed, ref totalElapsed, ref seconds, ref secondPassed);

                        yield return null;
                    }

                    InfectedVisionEffect(false);

                    TutorialRig.Instance.headTitles.CLOSEDISPLAY();
                }
                if (TeamManager.GetLocalTeam() == Infected)
                    MenuHelper.ShowNotification("Countdown Over", "GO AND INFECT THEM ALL!", 3.5f);
            }
            finally
            {
                HideVision = false;
            }

        }

        private static void DisplayCountdown(int num)
        {
            var target = Infection.Instance.CountdownValue.GetValue();
            var icon = Texture2D.whiteTexture;
            var sprite = Sprite.Create(icon, new Rect(0, 0, 0, 0), new Vector2(0.5f, 0.5f), 100f);
            var tutorialRig = TutorialRig.Instance;
            var headTitles = tutorialRig.headTitles;

            tutorialRig.gameObject.SetActive(true);
            headTitles.gameObject.SetActive(true);

            tutorialRig.headTitles.timeToScale = Mathf.Lerp(0.05f, 0.4f, Mathf.Clamp01(target - 1f));
            tutorialRig.headTitles.CUSTOMDISPLAY("Countdown, get ready...", num.ToString(), sprite, target);
            tutorialRig.headTitles.sr_element.sprite = sprite;
        }

        private static bool IsCountdownActive()
            => Config.CountdownLength.ClientValue != 0
                        && Infection.Instance.CountdownValue.GetValue() != 0
                        && !Infection.Instance.InfectedLooking.GetValue();

        private static bool IsAllowed()
            => Infection.Instance.IsStarted &&
                ((Infection.Instance.Config.ShowCountdownToAll.ClientValue && TeamManager.GetLocalTeam() != Infected)
                    || TeamManager.GetLocalTeam() == Infected);

        private static void InfectedVisionEffect(bool active)
        {
            if (TeamManager.GetLocalTeam() == Infected)
                VisionEffect(active);
        }

        private static void HandleTime(int target, ref float elapsed, ref float totalElapsed, ref int seconds, ref bool secondPassed)
        {

            // Check for second counter
            if (secondPassed)
            {
                int remainingSeconds = target - seconds;

                DisplayCountdown(remainingSeconds);

                secondPassed = false;
            }

            // Tick timer
            elapsed += TimeUtilities.DeltaTime;
            totalElapsed += TimeUtilities.DeltaTime;

            // If a second passed, send the notification next frame
            if (elapsed >= 1f)
            {
                elapsed--;
                seconds++;

                secondPassed = true;
            }
        }

        private static void VisionEffect(bool active)
        {
            LocalControls.LockedMovement = active;

            if (active)
                HideVision = true;

            LocalVision.BlindColor = Color.black;
            LocalVision.Blind = active;
            LocalHealth.MortalityOverride = !active && Infection.Instance.Infected.Metadata.Mortality.ClientValue;
        }
    }
}