using UnityEngine;

public class ProjectileSystem : MonoBehaviour
{
	private static ProjectileSystem instance;

	private int typeCount;
	private ProjectileType[] types;
	private SimpleTransform[][] projectiles;
	private int[] counts;
	
	// Avoid new allocation each frame
	private const int MAX_PROJECTILES_PER_TYPE = 1024;
	private readonly Matrix4x4[] toRender = new Matrix4x4[MAX_PROJECTILES_PER_TYPE];
	private readonly int[] toExplode = new int[MAX_PROJECTILES_PER_TYPE];

	public LayerMask hitMask;

	private ComponentPool<ParticleSystem>[] hitEffects;
	private ComponentPool<ParticleSystem>[] fireEffects;
	
	private void Awake()
	{
		instance = this;

		types = Resources.FindObjectsOfTypeAll<ProjectileType>();
		typeCount = types.Length;
		projectiles = new SimpleTransform[typeCount][];
		counts = new int[typeCount];
		hitEffects = new ComponentPool<ParticleSystem>[typeCount];
		fireEffects = new ComponentPool<ParticleSystem>[typeCount];
		for (int i = 0; i < typeCount; i++)
		{
			projectiles[i] = new SimpleTransform[MAX_PROJECTILES_PER_TYPE];
			counts[i] = 0;
			
			hitEffects[i] = new ComponentPool<ParticleSystem>(10, types[i].blastFx);
			fireEffects[i] = new ComponentPool<ParticleSystem>(10, types[i].fireFx);
		}
	}

	private void Update()
	{
		for (int t = 0; t <typeCount; t++)
		{
			var type = types[t];
			var list = projectiles[t];

			float step = type.speed * Time.deltaTime;
			float radius = type.collisionRadius;
			float damage = type.damage;
			float maxRange = type.maxRange;
			var mesh = type.mesh;
			var material = type.material;
			
			int toExplodeCount = 0;
			
			for (int i = 0; i < counts[t]; i++)
			{
				var current = list[i];
				
				RaycastHit hitInfo;
				if (Physics.SphereCast(current.position, radius, current.direction, out hitInfo, step, hitMask))
				{
					hitInfo.transform.GetComponent<Breakable>().Hit(damage);
					current.distance += hitInfo.distance;
					
					
					var blast = hitEffects[t].GetActive();
					blast.transform.position = list[toExplode[i]].position;
					blast.Play();
					
					toExplode[toExplodeCount++] = i;
					continue;
				}
				
				current.distance += step;
				if (current.distance > maxRange)
				{
					toExplode[toExplodeCount++] = i;
					continue;
				}
				
				// Remember to reset
				list[i] = current;
				toRender[i] = Matrix4x4.TRS(current.position, current.rotation, Vector3.one * radius);
			}
			
			SortReversed(toExplode, toExplodeCount);
			for (int i = 0; i < toExplodeCount; i++) {
				// TODO: use pool
				
				// Swap with last, the other one doesn't matter
				list[toExplode[i]] = list[counts[t] - 1];
				counts[t]--;
			}
			
			Graphics.DrawMeshInstanced(mesh, 0, material, toRender, counts[t]);
		}
		
	}

	public void Stop()
	{
		// Clear counts only
		for (int i = 0; i < typeCount; i++)
		{
			counts[i] = 0;
		}
	}
	
	public static void Shoot(Vector3 position, Quaternion rotation, ProjectileType type)
	{
		// We need to check initial collisions here, otherwise any enclosing collider would be missed
		var initialHits = Physics.OverlapSphere(position, type.collisionRadius, instance.hitMask);
		if (initialHits.Length > 0)
		{
			initialHits[0].GetComponent<Breakable>()?.Hit(type.damage);
			Instantiate(type.blastFx, position, Quaternion.identity);
			return;
		}

		int typeIndex = instance.GetTypeIndex(type);

		int index = instance.counts[typeIndex];
		instance.projectiles[typeIndex][index] = new SimpleTransform
		{
			start = position,
			rotation = rotation,
			direction = rotation * Vector3.forward
		};
		instance.counts[typeIndex]++;

		var blast = instance.fireEffects[typeIndex].GetActive();
		blast.transform.position = position;
		blast.Play();
	}

	private int GetTypeIndex(ProjectileType type)
	{
		for (int i = 0; i < typeCount; i++)
		{
			if (type == types[i])
				return i;
		}

		return -1;
	}

	private static void SortReversed(int[] array, int count)
	{
		for (int i = 0; i < count; i++)
		{
			for (int j = i; j < count; j++)
			{
				if (array[j] > array[i])
				{
					int temp = array[j];
					array[j] = array[i];
					array[i] = temp;
				}
			}
		}
	}
}

public struct SimpleTransform
{
	public Vector3 start;
	public Vector3 direction;
	public float distance;
	public Quaternion rotation;

	public Vector3 position => start + direction * distance;
}