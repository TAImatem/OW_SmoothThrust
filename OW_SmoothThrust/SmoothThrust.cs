using OWML.Common;
using OWML.ModHelper;
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
	}
	public class SmoothThrust : ModBehaviour
	{
		private void Start()
		{
			ModHelper.HarmonyHelper.AddPrefix<ThrusterModel>("AddTranslationalInput", typeof(ThrusterModel_SmoothPatch), "Prefix");
			ModHelper.HarmonyHelper.AddPostfix<ThrusterModel>("OnDestroy", typeof(ThrusterModel_SmoothPatch), "Postfix");
			ModHelper.Console.WriteLine("Smooth Thrust Ready!");
		}

		public static Dictionary<MonoBehaviour, SmoothCont> container = new Dictionary<MonoBehaviour, SmoothCont>();
		public static float _analogSmoothTime = 0.3f;
		public struct SmoothCont
		{
			public Vector3 _prevTranslation;
			public Vector3 _buildupVelocity;
		}
	}
}
