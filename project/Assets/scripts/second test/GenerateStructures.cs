using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GenerateStructures : MonoBehaviour
{
    public Transform parent;
    public GameObject objecst;
    public List<Vector3> queue;
    public List<Vector3> oldposs;

    public bool done = false;
    public List<GameObject> parents;
    public List<Vector3> pvs;
    public GameObject pp;
     List<grids> gs;
    public bool gennews = false;

    
    public List<List<Vector3>> help5 = new List<List<Vector3>>();
    public GameObject b = null;
    int a = 0;
    int runns = 0;
    Vector3 pos;
    int oldcount ;
    void Update()
    {
        if ( runns == 0)
        {
            for (int j = 0; j < parents.Count; j++)
            {
                for (int i = 0; i < queue.Count; i++)
                {
                    if (queue[i].x > (parents[j].transform.position.x - 138) && queue[i].x < (parents[j].transform.position.x + 138) && queue[i].z > (parents[j].transform.position.z - 138) && queue[i].z < (parents[j].transform.position.z + 138))
                    {
                        GameObject cube = Instantiate(objecst, new Vector3(queue[i].x, queue[i].y, queue[i].z), Quaternion.identity, parents[j].transform);

                        oldposs.Add(cube.transform.position);
                    }

                }


            }
            queue.Clear();


            runns++;
        }
        else if (queue.Count > 999 && runns > 0){
            runns = 0; 
        }


        if (done == false)
        {
            for (int i = 0; i < parents.Count; i++)
            {
                pvs.Add(new Vector3(parents[i].transform.position.x, parents[i].transform.position.y, parents[i].transform.position.z));


            }
            done = true;
        }
        oldcount++;
    }
    public void addds(Vector3 pos, Vector3 _position, float height,List<GameObject> _parent)
    {
        Vector3 Queue = new Vector3(pos.x + _position.x , height, pos.z + _position.z);
        Vector3 Poss2 = new Vector3(pos.x , height, pos.z );
        queue.Add(Queue);
        parents = _parent;

        a++;
    }
    public void adswithwater(GameObject pos, Transform parent)
    {
        Debug.Log(parent);
        for (int i = 0; i < queue.Count;i++)
        {
            pos.transform.parent = parent;
            pos.transform.position = new Vector3(queue[i].x, queue[i].y, queue[i].z);

        }

    }

    public void gen(Vector3 pos)
    {
        Instantiate(objecst, new Vector3(pos.x,pos.y, pos.z), Quaternion.identity,parent);
    }
    public void updateCity()
    {
        Debug.Log("delete");
        var children = new List<GameObject>();
        foreach (Transform child in parent) children.Add(child.gameObject);
        children.ForEach(child => DestroyImmediate(child));
    }
}
[System.Serializable]
public class grids{
    List<Vector3> list;

}
