using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class StorageBar : MonoBehaviour
{
    [SerializeField]
    private Slider storageBar;
    public Vector3 offset;

    // Start is called before the first frame update
    void Start()
    {
        storageBar = GetComponent<Slider>();
        Vector3 offset = transform.position;
    }

    // Update is called once per frame
    

    public void UpdateStorageBar(int storage, int maxStorage, bool flip)
    {
        storageBar.value = (float)storage / maxStorage;

        if (flip)
         
        {
            transform.parent.localPosition = new Vector3 (offset.x,offset.y,offset.z);
        }
        else
        {
            transform.parent.localPosition = new Vector3(-offset.x, offset.y, offset.z);
        }

    }
}
