/*
 * Author: Stefan Dieckmann
 * Date: 11 November 2018
 */

namespace Utilities
{
	/// <summary>
	/// Singleton that persists across multiple scenes
	/// </summary>
	public class PersistentSingleton<T> : Singleton<T> where T : Singleton<T>
	{
		public override void Awake()
		{
			base.Awake();
			DontDestroyOnLoad(gameObject);
		}
	}
}
