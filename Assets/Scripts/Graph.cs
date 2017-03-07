using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Graph : MonoBehaviour {

	[SerializeField] TextAsset _dataFile;

	[SerializeField] int _maxGraphSize = 200;

	[SpaceAttribute(10)]

	[SerializeField] GameObject _nodePrefab;
	[SerializeField] GameObject _edgePrefab;
	[SerializeField] float repulsion = 0.005f;

	List<Node> _nodes = new List<Node>();

	// Use this for initialization
	void Start () {
		TestLoadGraph();
	}

	void TestLoadGraph(){

		var nodeLookup = new Dictionary<string,Node>();

		var lines = _dataFile.text.Split(new string[]{"\r\n", "\r", "\n"}, StringSplitOptions.None);
		foreach (var line in lines){
			if (line.Trim().Length > 0){
				var tokens = line.Split(' ');
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
			if (_nodes.Count > _maxGraphSize)
				break;
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

	void FixedUpdate(){
		ApplyColombsLaw();
	}

    void ApplyColombsLaw(){
		int nodeCount = _nodes.Count;

		for (int i = 0; i < nodeCount; ++i){
			for (int j = i; j < nodeCount; ++j){
				if (i == j)
					continue;
				var Body1 = _nodes[i].Body;
				var Body2 = _nodes[j].Body;

				Vector3 d = Body1.position - Body2.position;
				float distance = d.magnitude + 0.001f;
				Vector3 direction = d.normalized;

				if (distance < 5){
					var force = (direction * repulsion) / (distance * distance * 0.5f);
					
					// Body1.AddForce(force);
					// Body2.AddForce(-force);
				}
			}
		}
    }

	Node CreateRandomNode(){
		var pos = UnityEngine.Random.insideUnitSphere * 100;
		GameObject n = (GameObject)Instantiate(_nodePrefab, pos, Quaternion.identity);
		_nodes.Add(n.GetComponent<Node>());
		return n.GetComponent<Node>();
	}

	void CreateEdge(Node n1, Node n2){
		GameObject edge = (GameObject)Instantiate(_edgePrefab);
		var spring = edge.GetComponent<GraphSpringJoint>();
		spring.Body1 = n1.Body;
		spring.Body2 = n2.Body;

		var render = edge.GetComponent<EdgeRenderer>();
		render.Body1 = n1.Body;
		render.Body2 = n2.Body;
	}

}
