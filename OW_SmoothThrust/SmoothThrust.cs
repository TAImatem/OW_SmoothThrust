using OWML.Common;
using OWML.ModHelper;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace TAI_SmoothThrust
{
	class ThrusterModel_SmoothPatch
	{
		public static void Prefix(ThrusterModel __instance, ref Vector3 input)
		{
			if (!SmoothThrust.container.ContainsKey(__instance))
			{
				SmoothThrust.container.Add(__instance, new SmoothThrust.SmoothCont());
			}
			SmoothThrust.SmoothCont iter = SmoothThrust.container[__instance];
			if (OWInput.IsNewlyPressed(InputLibrary.interact, InputMode.All))
			{
				iter._prevTranslation = input;
				iter._buildupVelocity = Vector3.zero;
			}
			if (input.magnitude > 0.0001f && OWInput.IsHeld(InputLibrary.interact, 0.05f, InputMode.All))
			{
				iter._prevTranslation.x = Mathf.SmoothDamp(iter._prevTranslation.x, input.x, ref iter._buildupVelocity.x, SmoothThrust._analogSmoothTime);
				iter._prevTranslation.z = Mathf.SmoothDamp(iter._prevTranslation.z, input.z, ref iter._buildupVelocity.z, SmoothThrust._analogSmoothTime);
				iter._prevTranslation.y = Mathf.SmoothDamp(iter._prevTranslation.y, input.y, ref iter._buildupVelocity.y, SmoothThrust._analogSmoothTime);
				input = iter._prevTranslation;
			}
			if (SmoothThrust.anglerSafety)
			{
				Vector3 myPos;
				if (__instance is ShipThrusterModel)
					myPos = SmoothThrust.ShipNMBody.GetCenterOfMass();
				else
					myPos = SmoothThrust.PlayerNMBody.GetCenterOfMass();

				float mindist = Mathf.Infinity;
				bool triggered = false;
				foreach (AnglerfishController fish in SmoothThrust.fishes)
				{
					if (fish.gameObject.activeSelf)
					{
						if (fish.GetAnglerState() == AnglerfishController.AnglerState.Chasing)
							triggered = true;
						float dist = Vector3.Distance(myPos, fish.transform.position);
						mindist = Mathf.Min(dist, mindist);
					}
				}
				if (triggered)
					mindist = Mathf.Infinity;
				SmoothThrust.limiter = Mathf.Infinity;
				if (__instance is ShipThrusterModel)
				{
					if (mindist <= 400.1f)
					{
						if (input.magnitude * __instance.GetMaxTranslationalThrust() >= mindist / 20f)
						{
							input = input.normalized * (mindist / 21f) / __instance.GetMaxTranslationalThrust();
						}
					}
				}
				else
				{
					if (mindist <= 210f)
					{
						if (input.magnitude * 100f * __instance.GetMaxTranslationalThrust() >= mindist * 3f)
						{
							input = input.normalized * (mindist / 35f) / __instance.GetMaxTranslationalThrust();
						}
					}
				}
			}
			iter._prevTranslation = input;
			SmoothThrust.container[__instance] = iter;
		}

		public static void Postfix(ThrusterModel __instance)
		{
			if (SmoothThrust.container.ContainsKey(__instance))
			{
				SmoothThrust.container.Remove(__instance);
			}
		}

		public static void Angler_Postfix(AnglerfishController __instance)
		{
			if (__instance.gameObject.activeSelf)
			{
				SmoothThrust.fishes.Add(__instance);
			}
			else
			{
				SmoothThrust.fishes.Remove(__instance);
			}
		}
	}
	public class SmoothThrust : ModBehaviour
	{
		private void Start()
		{
			ModHelper.HarmonyHelper.AddPrefix<ThrusterModel>("AddTranslationalInput", typeof(ThrusterModel_SmoothPatch), "Prefix");
			ModHelper.HarmonyHelper.AddPostfix<ThrusterModel>("OnDestroy", typeof(ThrusterModel_SmoothPatch), "Postfix");
			ModHelper.HarmonyHelper.AddPostfix<AnglerfishController>("OnSectorOccupantsUpdated", typeof(ThrusterModel_SmoothPatch), "Angler_Postfix");
			ModHelper.Console.WriteLine("Smooth Thrust Ready!");
		}
		public override void Configure(IModConfig config)
		{
			_analogSmoothTime = config.GetSettingsValue<float>("Smoothness");
			anglerSafety = config.GetSettingsValue<bool>("Angler safety");
		}

		private void Update()
		{
			if (LoadManager.GetCurrentScene() == OWScene.SolarSystem || LoadManager.GetCurrentScene() == OWScene.EyeOfTheUniverse)
			{
				if (PlayerNMBody == null)
					PlayerNMBody = GameObject.FindObjectOfType<PlayerNoiseMaker>().GetAttachedBody();

			}
			else
				PlayerNMBody = null;
			if (LoadManager.GetCurrentScene() == OWScene.SolarSystem)
			{
				if (ShipNMBody == null)
					ShipNMBody = GameObject.FindObjectOfType<ShipNoiseMaker>().GetAttachedBody();
			}
			else
			{
				fishes.Clear();
				ShipNMBody = null;
			}
		}

		public static Dictionary<MonoBehaviour, SmoothCont> container = new Dictionary<MonoBehaviour, SmoothCont>();
		public static float _analogSmoothTime = 0.3f;
		public static bool anglerSafety;
		public static HashSet<AnglerfishController> fishes = new HashSet<AnglerfishController>();
		public static OWRigidbody PlayerNMBody = null, ShipNMBody = null;

		public static float limiter;
		public static Vector3 latest;

		public struct SmoothCont
		{
			public Vector3 _prevTranslation;
			public Vector3 _buildupVelocity;
		}
	}
}
