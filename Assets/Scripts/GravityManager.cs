using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using Random = UnityEngine.Random;

[BurstCompile]
public struct CopyJob : IJobParallelForTransform
{
    public NativeArray<GBody> GBodies;
    public void Execute(int i, TransformAccess transform)
    {
        transform.position = GBodies[i].Position;
    }
}

[BurstCompile]
public struct GravityJob : IJobParallelFor
{
    public NativeArray<GBody> GBodies;
    [ReadOnly]
    public NativeArray<GBody> GBodies2;
    public float deltaTime;
    private const float G = 0.1f; //gravitational constant
    public void Execute(int i)
    {
        var b1 = GBodies[i];
        if (b1.Mass <= 0) return;
        float3 speedDelta = new float3();
        for (var j = 0; j < GBodies.Length; j++)
        {
            if (i == j) continue;
            var b2 = GBodies2[j];
            var distSqrt = math.lengthsq(b1.Position - b2.Position);
            var gStrength = G * b1.Mass * b2.Mass / distSqrt; //equation for universal gravitation
            speedDelta += (b2.Position - b1.Position) * gStrength;
        }
        b1.Speed += speedDelta / b1.Mass * deltaTime;
        b1.Position += b1.Speed;
        GBodies[i] = b1;
    }

}

public class GravityManager : MonoBehaviour
{
    // Public
    public GameObject StarPrefab;
    
    // Private
    private Camera _cam;
    private float _mouseDownTime;
    private Vector2 _mouseDownPos;
    
    // Native
    private TransformAccessArray _transformAccessArray;
    private NativeArray<GBody> _bodiesStruct;
    private NativeArray<GBody> _bodiesStruct2; //we need a copy of the data to read from to enable parallelization
    
    // Jobs
    private GravityJob _gravityJob;
    private CopyJob _copyJob;
    // Constants
    const int max_units = 3900;
    void Start()
    {
        _cam = Camera.main;
        
        var transforms = new Transform[max_units];
        const float spread = 4550f; //how far from origin we want to place the stars
        _bodiesStruct = new NativeArray<GBody>(max_units, Allocator.Persistent);
        _bodiesStruct2 = new NativeArray<GBody>(max_units, Allocator.Persistent);
        for (int i = 0; i < max_units; i++)
        {
            var newbd = Instantiate(StarPrefab).transform;
            var str = new GBody();
            str.Position = new Vector3(Random.value*spread-spread/2f, Random.value*spread-spread/2f,Random.value*spread-spread/2f);
            str.Mass = Random.value*15f + 1f;
            newbd.position = str.Position;
            newbd.localScale = str.Scale;
            transforms[i] = newbd;
            _bodiesStruct[i] = str;
        }
        
        _transformAccessArray = new TransformAccessArray(transforms);

        Time.fixedDeltaTime = 0.01f;
        _gravityJob = new GravityJob {GBodies = _bodiesStruct, GBodies2 = _bodiesStruct2, deltaTime = Time.fixedDeltaTime};
        _copyJob = new CopyJob {GBodies = _bodiesStruct};
    }


    void FixedUpdate()
    {
        // make a copy of the data
        for (int i = 0; i < max_units; i++)
        {
            _bodiesStruct2[i] = _bodiesStruct[i];
        }
        
        // update data
        _gravityJob.Schedule(max_units, 16 ).Complete();
        
        // assign position to transforms
        _copyJob.Schedule(_transformAccessArray).Complete();
    }
    
    private void CheckCollisions()
    {
    /*    var destruction = false;
        for (var i = 0; i < _bodies.Length; i++)
        {
            var b1 = _bodies[i];
            if (b1.Mass <= 0) continue;
            for (var j = 0; j < _bodies.Length; j++)
            {
                var b2 = _bodies[j];
                if (b1 == b2 || b2.Mass <= 0) continue;
                var dist = (b1.Position - b2.Position).magnitude;
                if (dist < b1.Radius + b2.Radius)
                {
                    var b1Ratio = b1.Mass / (b1.Mass + b2.Mass);
                    var b2Ratio = 1 - b1Ratio;
                    b1.transform.position = b1.transform.position * b1Ratio + b2.transform.position * b2Ratio;
                    b1.Mass += b2.Mass;
                    b1.Speed += b2.Speed*b2Ratio;
                    b1.UpdateScale();
                    b2.Mass = 0;
                    destruction = true;
                }
            }
        }

        if (destruction)
        {
            var bdlist = new List<GBody>();
            for (var i = 0; i < _bodies.Length; i++)
            {
                var b1 = _bodies[i];
                if (b1.Mass > 0)
                {
                    bdlist.Add(b1);
                }
                else
                {
                    Destroy(b1.gameObject);
                }
            }

            _bodies = bdlist.ToArray();
        }*/
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _mouseDownTime = Time.time;
            _mouseDownPos = Input.mousePosition;
        }

        /*if (Follow != null)
        {
            var pos = Follow.transform.position;
            pos.z = _cam.transform.position.z;
            _cam.transform.position = Vector3.Lerp(_cam.transform.position, pos, 0.01f);
            if( (_cam.transform.position - pos).magnitude > 30 )
            {
                _cam.transform.position = Vector3.Lerp(_cam.transform.position, pos, 0.3f);
            } 
        }*/

        if (Input.GetMouseButtonUp(0))
        {
            var elapsedTime = Time.time - _mouseDownTime;
            Plane plane = new Plane(Vector3.forward, 0);
            Vector3 worldPos = Vector3.zero;
            Vector3 worldPosEnd = Vector3.zero;
            float distance;
            Ray ray = _cam.ScreenPointToRay(_mouseDownPos);
            if (plane.Raycast(ray, out distance))
            {
                worldPos = ray.GetPoint(distance);
            }

            ray = _cam.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out distance))
            {
                worldPosEnd = ray.GetPoint(distance);
            }
            
         /*   var newbd = Instantiate(_bodies[0]);
            newbd.transform.position = worldPos;
            newbd.Mass = elapsedTime * 2 + 1f;
            newbd.Speed = (worldPos-worldPosEnd)/40f;
            newbd.UpdateScale();
            var bdlist = new List<GBody>(_bodies);
            bdlist.Add(newbd);
            _bodies = bdlist.ToArray();

            var m = _bodies[0].GetComponent<MeshRenderer>().material;
            var newm = new Material(m.shader);
            newm.color = Random.ColorHSV(0, 1, 0, 1, 0.6f, 1f);
            newbd.GetComponent<MeshRenderer>().material = newm;*/
        }
    }
    
    private void OnDestroy()
    {
        _bodiesStruct.Dispose();
        _bodiesStruct2.Dispose();
        _transformAccessArray.Dispose();
    }

}
