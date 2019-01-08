using System;
using UnityEngine;

public class Breakable : MonoBehaviour, IHittable
{
	private const float HP_LOW_THRESHOLD = 0.001f;
	
	public int maxHp = 10;
	public float hp { get; private set; }
	public bool broken { get; private set; }
	
	public event Action OnBreak;
	public bool doDestroyOnBreak;
	public ParticleSystem breakFXPrefab;
	
	private void Start()
	{
		hp = maxHp;
		broken = false;
	}

	public void Hit(float damage)
	{
		hp -= damage;

		if (hp < HP_LOW_THRESHOLD)
		{
			OnBreak?.Invoke();
			
			if (doDestroyOnBreak)
			{
				Destroy(gameObject);
			}

			if (breakFXPrefab != null)
			{
				Instantiate(breakFXPrefab, transform.position, Quaternion.identity);
			}

			hp = 0;
			broken = true;
		}
	}
}