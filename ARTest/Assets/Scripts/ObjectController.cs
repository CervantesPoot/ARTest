using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ObjectController : MonoBehaviour
{
    [SerializeField]
    private Camera aRCamera;
    [SerializeField]
    private GameObject model;

    [SerializeField]
    private Transform placeHolderModels;

    [SerializeField]
    private GameObject aRPointer;

    [SerializeField]
    private ARRaycastManager aRRayCastManager;

    [SerializeField]
    private GameObject mainMenu;

    [SerializeField]
    private GameObject editionMenu;

    [SerializeField]
    private float deadZone;

    [SerializeField]
    private float rotationMultiplier;
    [SerializeField]
    private float scaleMultiplier;

    private GameObject objectSelected = null;

    private bool isEditing;
    private bool isMoving;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private Vector2 initialPositionMix;
    private Vector2 initialPosition1;
    private Vector2 initialPosition2;

    // Update is called once per frame
    void Update()
    {
        if (isEditing)
        {
            /// One finger is for movement 
            if (Input.touchCount == 1)
            {
                Touch touchOne = Input.GetTouch(0);
                if (touchOne.phase == TouchPhase.Began)
                {
                    Vector2 tapPosition = touchOne.position;
                    if (OverUI(tapPosition) == false && OverModel(tapPosition) == true)
                    {
                        isMoving = true;
                    }
                    else
                    {
                        isMoving = false;
                    }
                }
                if (touchOne.phase == TouchPhase.Moved && isMoving == true)
                {
                    /// move an object over a plane
                    if (aRRayCastManager.Raycast(touchOne.position, hits, TrackableType.Planes))
                    {
                        Pose hitPose = hits[0].pose;
                        aRPointer.transform.position = hitPose.position;
                    }
                }
            }
            /// Two fingers is for scale
            if (Input.touchCount == 2)
            {
                Touch touchOne = Input.GetTouch(0);
                Touch touchTwo = Input.GetTouch(1);
                if (touchOne.phase == TouchPhase.Began || touchTwo.phase == TouchPhase.Began)
                {
                    initialPosition1 = touchOne.position;
                    initialPosition2 = touchTwo.position;
                }

                if (touchOne.phase == TouchPhase.Moved || touchTwo.phase == TouchPhase.Moved)
                {
                    Vector2 currentTouch1 = touchOne.position;
                    Vector2 currentTouch2 = touchTwo.position;

                    float oldDistance = Vector2.Distance(initialPosition1, initialPosition2);
                    float newDistance = Vector2.Distance(currentTouch1, currentTouch2);
                    initialPosition1 = currentTouch1;
                    initialPosition2 = currentTouch2;

                    if (Mathf.Abs(newDistance - oldDistance) <= deadZone)
                    {
                        return;
                    }

                    /// scale up
                    if (oldDistance < newDistance)
                    {
                        objectSelected.transform.localScale += Vector3.one * scaleMultiplier;
                    }
                    /// scale Down
                    else if (oldDistance > newDistance)
                    {
                        objectSelected.transform.localScale += Vector3.one * scaleMultiplier * -1;
                    }
                }
            }
            /// Three fingers is for scale
            if (Input.touchCount == 3)
            {
                Touch touchOne = Input.GetTouch(0);
                Touch touchTwo = Input.GetTouch(1);
                Touch touchThree = Input.GetTouch(2);
                if (touchOne.phase == TouchPhase.Began || touchTwo.phase == TouchPhase.Began || touchThree.phase == TouchPhase.Began)
                {
                    initialPositionMix = touchThree.position - touchTwo.position - touchOne.position;
                }

                else if (touchOne.phase == TouchPhase.Moved || touchTwo.phase == TouchPhase.Moved || touchThree.phase == TouchPhase.Moved)
                {
                    Vector2 currentTouchMix = touchThree.position - touchTwo.position - touchOne.position;

                    float angleY = (currentTouchMix.x - initialPositionMix.x) * rotationMultiplier;
                    float angleCamera = (currentTouchMix.y - initialPositionMix.y) * rotationMultiplier;

                    if (Mathf.Abs(currentTouchMix.x - initialPositionMix.x) <= deadZone)
                    {
                        angleY = 0;
                    }

                    if (Mathf.Abs(currentTouchMix.y - initialPositionMix.y) <= deadZone)
                    {
                        angleCamera = 0;
                    }

                    initialPositionMix = currentTouchMix;

                    if (angleY != 0)
                    {
                        ResetRotationPivot();
                        objectSelected.transform.Rotate(Vector3.up * angleY);
                    }
                    // objectSelected.transform.rotation *= Quaternion.Euler(Vector3.up * angleY);
                    if (angleCamera != 0)
                    {
                        AdjustRotationPivot();
                        objectSelected.transform.Rotate(Vector3.right * angleCamera);
                    }

                }
            }
            return;
        }
        if (Input.touchCount == 1)
        {
            /// check if a 3d object was selected
            Touch tap = Input.GetTouch(0);
            if (tap.phase == TouchPhase.Began)
            {
                Vector2 tapPosition = tap.position;
                if (OverUI(tapPosition) == false && OverModel(tapPosition) == true)
                {
                    isMoving = true;
                    ShowEditionMenu();
                }
            }
        }
    }

    private bool OverUI(Vector2 screenPosition)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPosition;

        List<RaycastResult> result = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, result);

        return result.Count > 0;
    }

    private bool OverModel(Vector2 screenPosition)
    {
        Ray ray = aRCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hitModel))
        {
            if (hitModel.collider.CompareTag("Object"))
            {
                EditModel(hitModel.collider.transform.parent.gameObject);
                return true;
            }
        }

        return false;
    }

    private void AdjustRotationPivot()
    {
        List<GameObject> childs = new List<GameObject>();
        foreach (Transform child in objectSelected.transform)
        {
            childs.Add(child.gameObject);
        }
        foreach (GameObject child in childs)
        {
            child.transform.parent = objectSelected.transform.parent;
        }

        Vector2 middleScreen = new Vector2(Screen.width / 2, Screen.height / 2);
        objectSelected.transform.rotation = Quaternion.LookRotation(aRCamera.ScreenToWorldPoint(middleScreen) - objectSelected.transform.position);

        foreach (GameObject child in childs)
        {
            child.transform.parent = objectSelected.transform;
        }
    }

    private void ResetRotationPivot()
    {
        List<GameObject> childs = new List<GameObject>();
        foreach (Transform child in objectSelected.transform)
        {
            childs.Add(child.gameObject);
        }
        foreach (GameObject child in childs)
        {
            child.transform.parent = objectSelected.transform.parent;
        }

        Vector2 middleScreen = new Vector2(Screen.width / 2, Screen.height / 2);
        objectSelected.transform.rotation = Quaternion.LookRotation(Vector3.zero);

        foreach (GameObject child in childs)
        {
            child.transform.parent = objectSelected.transform;
        }
    }

    public void CreateModel()
    {
        objectSelected = Instantiate(model);

        Vector2 middleScreen = new Vector2(Screen.width / 2, Screen.height / 2);
        aRRayCastManager.Raycast(middleScreen, hits, TrackableType.Planes);
        if (hits.Count > 0)
        {
            aRPointer.transform.position = hits[0].pose.position;
            // aRPointer.transform.rotation = hits[0].pose.rotation;            
        }

        objectSelected.transform.position = aRPointer.transform.position;
        objectSelected.transform.parent = aRPointer.transform;

        ShowEditionMenu();
    }

    public void ConfirmEdition()
    {
        objectSelected.transform.parent = placeHolderModels;

        ShowMainMenu();
    }

    public void DestroyModel()
    {
        Destroy(objectSelected);

        ShowMainMenu();
    }

    private void EditModel(GameObject reference)
    {
        objectSelected = reference;
        aRPointer.transform.position = objectSelected.transform.position;
        objectSelected.transform.parent = aRPointer.transform;

        ShowEditionMenu();
    }

    private void ShowMainMenu()
    {
        isEditing = false;
        editionMenu.SetActive(false);
        mainMenu.SetActive(true);
        objectSelected = null;
        aRPointer.SetActive(false);
    }

    private void ShowEditionMenu()
    {
        isEditing = true;
        editionMenu.SetActive(true);
        mainMenu.SetActive(false);
        aRPointer.SetActive(true);
    }

}
