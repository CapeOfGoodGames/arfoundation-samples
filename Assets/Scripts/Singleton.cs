/*
 * Author: Stefan Dieckmann
 * Date: 11 November 2018
 */

using UnityEngine;
using System;

namespace Utilities
{
	/// <summary>
	/// Singleton class
	/// </summary>
	/// <typeparam name="T">Type of the singleton</typeparam>
	public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
	{
		private static T _instance;

		/// <summary>
		/// The static reference to the instance
		/// </summary>
		public static T Instance
		{
			get
			{
				return _instance;
			}
			protected set
			{
				_instance = value;
			}
		}

		/// <summary>
		/// Gets whether an instance of this singleton exists
		/// </summary>
		public static bool InstanceExists { get { return _instance != null; } }

		public static event Action InstanceSet;

		/// <summary>
		/// Awake method to associate singleton with instance
		/// </summary>
		public virtual void Awake()
		{
			if (_instance != null)
			{
				Destroy(gameObject);
			}
			else
			{
				_instance = (T)this;
				if (InstanceSet != null)
				{
					InstanceSet();
				}
			}
		}

		/// <summary>
		/// OnDestroy method to clear singleton association
		/// </summary>
		public virtual void OnDestroy()
		{
			if (_instance == this)
			{
				_instance = null;
			}
		}
	}
}
