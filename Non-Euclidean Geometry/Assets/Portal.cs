using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Net.NetworkInformation;
using Unity.VisualScripting;
using UnityEngine;

public class Portal : MonoBehaviour
{
    [SerializeField]
    Camera playerCamera;

    [SerializeField]
    Camera portalCameraPrefab;

    [SerializeField]
    Shader portalShader;

    [SerializeField]
    MeshRenderer portalRenderer;
    
    RenderTexture portalTexture;
    Material portalMaterial;

    [SerializeField]
    Portal otherPortal;

    Camera portalCamera;

    [SerializeField]
    bool IsMirrorPortal;

    float direction = 1;

    // Start is called before the first frame update
    void Start()
    {
        portalTexture = new RenderTexture(playerCamera.pixelWidth, playerCamera.pixelHeight, 32);
        portalMaterial = new Material(portalShader);
        portalMaterial.SetTexture("_MainTex", portalTexture);
        portalRenderer.material = portalMaterial;

        portalCamera = Instantiate<Camera>(portalCameraPrefab);
        portalCamera.targetTexture = portalTexture;

        if (IsMirrorPortal)
        {
            direction = -1;
        }
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 playerPositionFromPortal = transform.worldToLocalMatrix.MultiplyPoint3x4(playerCamera.transform.position);
        Vector3 portalCameraPosition = otherPortal.transform.localToWorldMatrix.MultiplyPoint3x4(playerPositionFromPortal);

        Quaternion playerRotationFromPortal = Quaternion.Inverse(transform.rotation) * playerCamera.transform.rotation;
        Quaternion portalCameraRotation = otherPortal.transform.rotation * playerRotationFromPortal;

        portalCamera.transform.position = portalCameraPosition;
        portalCamera.transform.rotation = portalCameraRotation;


        Vector3 targetPoint = otherPortal.transform.position;

        Ray cameraRay = new Ray(portalCamera.transform.position, portalCamera.transform.forward);
        Plane portalPlane = new Plane(otherPortal.transform.forward * direction, otherPortal.transform.position);

        float intersectionDistance = 0;

        if(portalPlane.Raycast(cameraRay, out intersectionDistance)){
            targetPoint = cameraRay.GetPoint(intersectionDistance);
        }

        Vector3 cameraSpacePoint = portalCamera.worldToCameraMatrix.MultiplyPoint3x4(targetPoint);
        Vector3 cameraSpaceNormal = portalCamera.worldToCameraMatrix.MultiplyVector(portalPlane.normal);

        portalCamera.projectionMatrix = CalculateObliqueProjectionMatrix(cameraSpacePoint, cameraSpaceNormal);
    }

    Matrix4x4 CalculateObliqueProjectionMatrix(Vector3 point, Vector3 normal)
    {
        Matrix4x4 projectionMatrix = portalCamera.projectionMatrix;

        Vector4 M1 = projectionMatrix.GetRow(0);
        Vector4 M2 = projectionMatrix.GetRow(1);
        Vector4 M4 = projectionMatrix.GetRow(3);

        //http://www.terathon.com/lengyel/Lengyel-Oblique.pdf
        //A plane C is mathematically represented by a four-dimensional vector of the form
        // C = Nx, Ny, Nz, dot(-N, Q)
        //where N is the normal vector pointing away from the front side of the plane, and Q is any point lying in the plane itself.

        Vector4 C = new Vector4(normal.x, normal.y, normal.z, Vector3.Dot(-normal, point));
        Vector4 clipSpaceC = projectionMatrix.inverse.transpose * C;

        Vector4 clipSpaceQ = new Vector4(Mathf.Sign(clipSpaceC.x), Mathf.Sign(clipSpaceC.y), 1, 1);
        Vector4 Q = projectionMatrix.inverse * clipSpaceQ;

        Vector4 M3 = (2 * (Vector4.Dot(M4, Q)) / (Vector4.Dot(C, Q))) * C - M4;
        
        Matrix4x4 obliqueProjectionMatrix = new Matrix4x4();

        obliqueProjectionMatrix.SetRow(0, M1);
        obliqueProjectionMatrix.SetRow(1, M2);
        obliqueProjectionMatrix.SetRow(2, M3);
        obliqueProjectionMatrix.SetRow(3, M4);

        return obliqueProjectionMatrix;
    }
}
