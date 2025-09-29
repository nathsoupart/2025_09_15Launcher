using UnityEngine;

public class LookAtCamera : MonoBehaviour
{

    private Transform cameraTransform;

    void Start()
    {
        
        cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        if (cameraTransform is not null)
        {
            
            transform.LookAt(cameraTransform);

            // Optionnel : Si besoin, on peut inverser la rotation sur certains axes
            // Par exemple, pour ne pas que l'objet bascule à l'envers :
            Vector3 euler = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(0, euler.y, 0);
        }
    }
}
  