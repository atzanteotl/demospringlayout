using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphLayoutShader : MonoBehaviour {

	[SerializeField] TextAsset _dataFile;

	[SerializeField] int _maxGraphSize = 200;

	[SpaceAttribute(10)]

	[SerializeField] GameObject _nodePrefab;
	[SerializeField] GameObject _edgePrefab;
	
    [SerializeField] float repulsion = 0.005f;
	[SerializeField] float _damping = 0.005f;
	[SerializeField] float _springLength = 0.5f;
    [SerializeField] float springk = 12.2f;

	int[] adjacencyMatrix;
	ComputeBuffer adjacencyMatrixBuffer;

	List<Node> _nodes = new List<Node>();

	List<Edge> _edges = new List<Edge>();

	public ComputeShader cs;
	public ComputeShader colombsShader;
	public RenderTexture renderTarget;

	struct NodeDS {
		public Vector3 pos;
		public Vector3 acc;
		public Vector3 vel;
	}

	struct EdgeDS {
		public int id1;
		public int id2;
	}

	Vector3[] nodeAcc;
	ComputeBuffer nodeAccBuffer;

    Vector3[] nodeVel;
    ComputeBuffer nodeVelBuffer;


	EdgeDS[] shaderEdges;
	NodeDS[] shaderInput;
	NodeDS[] shaderOutput;
	ComputeBuffer dataBuffer;
	ComputeBuffer edgeBuffer;

	// Use this for initialization
	void Start () {
        Camera.main.useOcclusionCulling = false;
		TestLoadGraph();
	}

	private void OnDestroy()
	{
        dataBuffer.Dispose();
        nodeAccBuffer.Dispose();
        nodeVelBuffer.Dispose();
        edgeBuffer.Dispose();
	}

	void TestLoadGraph(){

		int kern = cs.FindKernel("HookesLaw");
        print(kern);

        cs.SetFloat("springLength", _springLength);
        cs.SetFloat("springk", springk);

		//RenderTexture tex = new RenderTexture(256, 256, 24);
		// renderTarget.enableRandomWrite = true;
		renderTarget.Create();
//
//		cs.SetTexture(kern, "res", renderTarget);
//		cs.Dispatch(kern, 50, 50, 1);
//
		var nodeLookup = new Dictionary<string,Node>();

        var sr = new System.IO.StringReader(_dataFile.text);
		//var lines = _dataFile.text.Split(new string[]{"\r\n", "\r", "\n"}, StringSplitOptions.None);
		while (true){
            var line = sr.ReadLine();
            if (line == null)
                break;
			if (line.Trim().Length > 0){
                var tokens = line.Split(new string[]{"\t", " "}, StringSplitOptions.None);
				var n1tok = tokens[0];
				var n2tok = tokens[1];

				if (!nodeLookup.ContainsKey(n1tok)){
					nodeLookup[n1tok] = CreateRandomNode();					 
				}
				if (!nodeLookup.ContainsKey(n2tok)){
					nodeLookup[n2tok] = CreateRandomNode();					 
				}

				CreateEdge(nodeLookup[n1tok], nodeLookup[n2tok]);
			}
			if (_nodes.Count >= _maxGraphSize)
				break;
		}

        print("nodes: " + _nodes.Count);

		int nodeCount = _nodes.Count;
		adjacencyMatrix = new int[_nodes.Count * _nodes.Count];

		nodeAcc = new Vector3[_nodes.Count * _nodes.Count];
        nodeVel = new Vector3[_nodes.Count];
        nodeAccBuffer = new ComputeBuffer (nodeAcc.Length, 3 * 4);
        nodeVelBuffer = new ComputeBuffer (nodeVel.Length, 3 * 4);

		for (int i = 0; i < nodeAcc.Length; ++i) {
			nodeAcc [i] = new Vector3 ();
			adjacencyMatrix [i] = new int ();
		}

        for (int i = 0; i < nodeVel.Length; ++i){
            nodeVel[i] = new Vector3();
        }



		shaderInput = new NodeDS[_nodes.Count];
		for (int i = 0; i < _nodes.Count; ++i) {
			shaderInput[i] = new NodeDS
            {
                pos = _nodes[i].pos,
                acc = new Vector3(),
                vel = _nodes[i].vel
            };
        }
		dataBuffer = new ComputeBuffer (shaderInput.Length, 3 * 4 * 3);
		dataBuffer.SetData (shaderInput);

		shaderEdges = new EdgeDS[_edges.Count];
		for (int i = 0; i < _edges.Count; ++i) {
			shaderEdges [i] = new EdgeDS ();
			shaderEdges [i].id1 = _nodes.IndexOf (_edges [i].Body1);
			shaderEdges [i].id2 = _nodes.IndexOf (_edges [i].Body2);
			adjacencyMatrix [nodeCount * shaderEdges [i].id2 + shaderEdges [i].id1] = 1;
		}
		edgeBuffer = new ComputeBuffer (shaderEdges.Length, 4 * 2);
		edgeBuffer.SetData (shaderEdges);

		adjacencyMatrixBuffer = new ComputeBuffer (adjacencyMatrix.Length, 4);
		adjacencyMatrixBuffer.SetData (adjacencyMatrix);

        for (int i = 0; i < nodeCount; ++i)
        {
            shaderInput[i].pos = _nodes[i].pos;
            shaderInput[i].acc = new Vector3();
            shaderInput[i].vel = _nodes[i].vel;
        }
	}

	void TestRandomGraph(){
		// create some nodes
		for (int i = 0; i < _maxGraphSize; ++i){
			CreateRandomNode();
		}

		for (int i = 0; i < _maxGraphSize / 5; ++i){
			Node n1 = _nodes[UnityEngine.Random.Range(0, _nodes.Count)];
			Node n2 = _nodes[UnityEngine.Random.Range(0, _nodes.Count)];
			if (n1 != n2){
				CreateEdge(n1, n2);
			}
		}
	}

    static bool skip = true;

	void Update(){
        if (false)
        {
            if (!skip)
            {
                CalcSum();
				UpdatePosition2();
                
            }
            skip = false;
            ApplyHookesLaw2();
            SumResults();
                
            ApplyColombsLaw();        
        }
        else
        {
            ApplyHookesLaw1();
            ApplyColombsLaw();

            UpdateVelocity();
            UpdatePosition();
        }
	}


	void ApplyHookesLaw1(){
		
		foreach (var e in _edges){
			Vector3 d = e.Body2.position - e.Body1.position;
			float displacement = _springLength - d.magnitude;
			Vector3 direction = d.normalized;

			e.Body1.AddForce(springk * direction * displacement * -0.5f);
			e.Body2.AddForce(springk * direction * displacement * 0.5f);
		}
	}

	void ApplyHookesLaw2(){

        int nodeCount = _nodes.Count;

		int hookesKern = cs.FindKernel("HookesLaw");

        cs.SetInt ("stride", nodeCount);
		cs.SetBuffer (hookesKern, "dataBuffer", dataBuffer);
		cs.SetBuffer (hookesKern, "edgeDataBuffer", edgeBuffer);
		cs.SetBuffer (hookesKern, "adjacencyMatrix", adjacencyMatrixBuffer);
		cs.SetBuffer (hookesKern, "accBuffer", nodeAccBuffer);

        cs.Dispatch (hookesKern, nodeCount / 16, nodeCount / 16, 1);
	}

    void SumResults()
    {
        int nodeCount = _nodes.Count;

		int accKern = cs.FindKernel("ComputeAcc");
  
        nodeVelBuffer.SetData(nodeVel);

        cs.SetInt("stride", nodeCount);
        cs.SetBuffer(accKern, "dataBuffer", dataBuffer);
        cs.SetBuffer(accKern, "accBuffer", nodeAccBuffer);
        cs.SetBuffer(accKern, "velBuffer", nodeVelBuffer);

        cs.Dispatch(accKern, nodeCount / 16, 1, 1);


    }

    void CalcSum()
    {
        return;
        int nodeCount = _nodes.Count;

        //shaderBuffer.GetData(shaderInput);
        nodeVelBuffer.GetData(nodeVel);

        for (int i = 0; i < nodeCount; ++i)
        {
            //_nodes[i].acc = shaderInput[i].acc;
            _nodes[i].pos = nodeVel[i];
            //_nodes[i].transform.position = nodeVel[i];
            if (float.IsNaN(_nodes[i].pos.x))
            {
                print("BLAM4");
                Debug.Break();
            }
        }
    }

    void ApplyColombsLaw(){
		//return;
		// copy data buffer
//		for (int i = 0; i < _nodes.Count; ++i) {			
////			shaderInput [i].pos = _nodes [i].pos;
////			shaderInput [i].acc = new Vector3 ();
//		}
//		dataBuffer.SetData(shaderInput);
////

		//int kern = colombsShader.FindKernel("ColumbsAlgo");
		//colombsShader.SetBuffer (kern, "dataBuffer", dataBuffer);
		//colombsShader.Dispatch (kern, shaderInput.Length, shaderInput.Length, 1);
		//dataBuffer.GetData (shaderInput);

		//for (int i = 0; i < shaderInput.Length; ++i) {
		//	_nodes [i].acc = shaderInput [i].acc;
		//}

		//return;

		int nodeCount = _nodes.Count;

		for (int i = 0; i < nodeCount; ++i){
			for (int j = i; j < nodeCount; ++j){
				if (i == j)
					continue;

				var n1 = _nodes[i];
				var n2 = _nodes[j];

				Vector3 d = n1.position - n2.position;
				float distance = d.magnitude + 0.001f;
				Vector3 direction = d.normalized;

				if (distance < 115){
					var force = (direction * repulsion) / (distance * distance * 0.5f);
					
					n1.AddForce(force);
					n2.AddForce(-force);
				}
			}
		}
    }

	void UpdateVelocity(){
		for (int i = 0; i < _nodes.Count; ++i){			
			Node n = _nodes [i];
			n.vel = (n.vel + n.acc * Time.deltaTime) * _damping;
			if (float.IsNaN (n.pos.x)) {
				print ("BLAM2");
			}
			n.acc = new Vector3();
		}
	}

	void UpdatePosition() {
        int nodeCount = _nodes.Count;
        float dt = Time.deltaTime;
		for (int i = 0; i < nodeCount; ++i){
			Node n = _nodes [i];
			if (float.IsNaN (n.pos.x)) {
				print ("BLAM3");
                Debug.Break();
			}
            n.pos += n.vel * dt;
			n.transform.position = n.pos;
		}
	}

    
    void UpdatePosition2() {
        int nodeCount = _nodes.Count;
        float dt = Time.deltaTime;
        for (int i = 0; i < nodeCount; ++i){
            Node n = _nodes [i];
            if (float.IsNaN (n.pos.x)) {
                print ("BLAM3");
                Debug.Break();
            }
            n.transform.position = n.pos;
        }
    }

	Node CreateRandomNode(){
		var pos = UnityEngine.Random.insideUnitSphere * 20
                             ;
		GameObject obj = (GameObject)Instantiate(_nodePrefab, pos, Quaternion.identity);
        obj.transform.SetAsFirstSibling();
		Node n = obj.GetComponent<Node>();
		n.pos = pos;
		n.acc = new Vector3();
		n.vel = new Vector3 ();
		_nodes.Add(n);
		return n;
	}

	void CreateEdge(Node n1, Node n2){
		GameObject edge = (GameObject)Instantiate(_edgePrefab);
		var spring = edge.GetComponent<Edge>();
		spring.Body1 = n1;
		spring.Body2 = n2;

		_edges.Add(spring);

		var render = edge.GetComponent<EdgeRenderer>();
		render.Body1 = n1.transform;
		render.Body2 = n2.transform;
	}
}
