using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;


public class MetaBallsHandler : MonoBehaviour
{
    [SerializeField] private RenderTexture renderTexture;
    [SerializeField] private GameObject UIPanel;
    [SerializeField] private RawImage rawImage;
    [SerializeField] private TMP_Text pauseButtonText;
    [SerializeField] private TMP_InputField radiusInput;
    [SerializeField] private TMP_Text colorModeButtonText;
    [SerializeField] private FlexibleColorPicker colorPicker;
    [SerializeField] private ComputeShader metaBallsComputeShader;
    [SerializeField] private ComputeShader cursorComputeShader;
    [SerializeField] private ComputeShader clearScreenComputeShader;

    [Space]

    [Range(0f, 10f), SerializeField] private float surface;
    [SerializeField] private float newRadius;
    [SerializeField] private Vector3 color = new Vector3(1, 1, 1);
    [SerializeField] private bool linearMode = false;
    
    [SerializeField] private Vector2 minAndMaxVel;

    private bool transformBalls = false;

    private List<MetaBall> metaBalls;
    private int nearestMetaBall = -1;
    
    [SerializeField] private uint width;
    [SerializeField] private uint height;
    [Space]
    [SerializeField] private float cursorThickness;
    private void Awake() {
        metaBalls = new List<MetaBall>();
        renderTexture = new RenderTexture((int)width, (int)height, 0);
        renderTexture.enableRandomWrite = true;
    }
    private void Start() {
        nearestMetaBall = -1;
        radiusInput.text = newRadius.ToString();
        rawImage.texture = renderTexture;
        metaBallsComputeShader.SetTexture(0, "Result", renderTexture);
        cursorComputeShader.SetTexture(0, "Result", renderTexture);
        clearScreenComputeShader.SetTexture(0, "Result", renderTexture);
        color = new Vector3(1, 1, 1);
    }
    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
        if (Input.GetKeyDown(KeyCode.I)) UIPanel.SetActive(!UIPanel.activeSelf);
        bool takeMouseInput = TakeMouseInput();
        ShowCursor(!takeMouseInput);
        SetRadiusScrollDelta();
        if (!Input.GetMouseButton(1)) SetNearestMetaBall();
        if (Input.GetKeyDown(KeyCode.R)) SetRandomVelocities();
        if (Input.GetKeyDown(KeyCode.Space)) PauseUnpauseSim();
    
        if (Input.GetMouseButtonDown(0) && takeMouseInput) SpawnMetaBall();
        if (nearestMetaBall != -1 && Input.GetMouseButtonDown(2)){
            metaBalls.RemoveAt(nearestMetaBall);
            nearestMetaBall = -1;
        }
        if (nearestMetaBall != -1 && !transformBalls && Input.GetMouseButton(1)){
            Vector2Int mousePos = GetCursorPos();
            MetaBall nearestBall = metaBalls[nearestMetaBall];
            metaBalls[nearestMetaBall] = new MetaBall(mousePos, nearestBall.vel, nearestBall.r, nearestBall.color);
        } 
        if (transformBalls) TransformBalls();
        SetupAndDispatchClearScreenComputeShader();
        if (metaBalls.Count > 0) SetupAndDispatchMetaBallsComputeShader();
        if (!Input.GetMouseButton(1))SetupAndDispatchCursorComputeShader();
    }
    private void SetupAndDispatchMetaBallsComputeShader(){
        metaBallsComputeShader.SetBool("linearMode", linearMode);
        metaBallsComputeShader.SetFloat("surface", surface);

        metaBallsComputeShader.SetInt("metaBallCount", metaBalls.Count);        
        ComputeBuffer metaBallsCB = new ComputeBuffer(metaBalls.Count, 8 * sizeof(float));
        metaBallsCB.SetData(metaBalls);
        metaBallsComputeShader.SetBuffer(0, "metaBalls", metaBallsCB);
        
        metaBallsComputeShader.Dispatch(0, (int)width / 8, (int)height / 8, 1);
        metaBallsCB.Dispose();
    }
    private void SetupAndDispatchCursorComputeShader(){
        Vector2Int mousePos = GetCursorPos();
        cursorComputeShader.SetInt("posX", mousePos.x);
        cursorComputeShader.SetInt("posY", mousePos.y);
        cursorComputeShader.SetFloat("newRadius", newRadius);
        cursorComputeShader.SetFloat("thickness", cursorThickness);
        cursorComputeShader.Dispatch(0, (int)width / 8, (int)height / 8, 1);
    }
    private void SetupAndDispatchClearScreenComputeShader(){
        clearScreenComputeShader.Dispatch(0, (int)width / 8, (int)height / 8, 1);
    }
    private void SpawnMetaBall(){
        Vector2 randVel = new Vector2(
        Random.Range(minAndMaxVel.x, minAndMaxVel.y),
        Random.Range(minAndMaxVel.x, minAndMaxVel.y));
        metaBalls.Add(new MetaBall(GetCursorPos(), randVel, newRadius, color));
    }
    private void SetNearestMetaBall(){
        Vector2Int mousePos = GetCursorPos();
        int nearestBall = -1;
        float nearestDistance = Mathf.Infinity;

        for (int i = 0; i < metaBalls.Count; i++){
            MetaBall ball = metaBalls[i];
            float distance = Vector2.Distance(mousePos, ball.pos);
            if (distance < nearestDistance && distance <= ball.r / surface){
                nearestDistance = distance;
                nearestBall = i;
            }
        }
        nearestMetaBall = nearestBall;
    }
    private Vector2Int GetCursorPos(){
        int x = Mathf.RoundToInt(((float)width / Screen.width) * Input.mousePosition.x);
        int y = Mathf.RoundToInt(((float)height / Screen.height) * Input.mousePosition.y); 
        return new Vector2Int(x, y);
    }
    private void SetRadiusScrollDelta(){
        newRadius += Input.mouseScrollDelta.y;
        if (Input.mouseScrollDelta.y != 0)
            radiusInput.text = newRadius.ToString();
    }
    public void SetRadiusInput(){
        string text = radiusInput.text;
        float flt;
        if(float.TryParse(text, out flt)){
            newRadius = flt;
        }
    }
    public void SetRandomVelocities(){
        for (int i = 0; i < metaBalls.Count; i++){
            MetaBall metaBall = metaBalls[i];
            Vector2 randVel = new Vector2(
            Random.Range(minAndMaxVel.x, minAndMaxVel.y),
            Random.Range(minAndMaxVel.x, minAndMaxVel.y));
            metaBalls[i] = new MetaBall(metaBall.pos, randVel, metaBall.r, metaBall.color);
        }
    }

    private void TransformBalls(){
        for (int i = 0; i < metaBalls.Count; i++){
            MetaBall metaBall = metaBalls[i];

            if (metaBall.pos.x - metaBall.r <= 0 || metaBall.pos.x + metaBall.r >= width){
                metaBall.vel.x *= -1;
            }
            if (metaBall.pos.y - metaBall.r <= 0 || metaBall.pos.y + metaBall.r >= height){
                metaBall.vel.y *= -1;
            }
            metaBall.pos += metaBall.vel * Time.deltaTime;

            metaBalls[i] = new MetaBall(metaBall.pos, metaBall.vel, metaBall.r, metaBall.color);
        }
    }
    public void Clear(){
        metaBalls.Clear();
        nearestMetaBall = -1;
    }
    private bool TakeMouseInput(){
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
        pointerEventData.position = Input.mousePosition;
        List<RaycastResult> rayCastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, rayCastResults);

        foreach (RaycastResult rayCastResult in rayCastResults){
            if (rayCastResult.gameObject.CompareTag("UI")){
                return false;
            }
        }
        return true;
    }
    private void ShowCursor(bool state){
        Cursor.visible = state;
    }
    public void PauseUnpauseSim(){
        transformBalls = !transformBalls;
        if (pauseButtonText.text == "Pause") pauseButtonText.text = "Unpause";
        else pauseButtonText.text = "Pause";
    }
    public void SetColor(){
        color = new Vector3(colorPicker.color.r, colorPicker.color.g, colorPicker.color.b);
    }
    public void SetColorMode(){
        linearMode = !linearMode;
        if (colorModeButtonText.text == "Inverse") colorModeButtonText.text = "Linear";
        else colorModeButtonText.text = "Inverse";
    }
}

struct MetaBall{
    public Vector2 pos;
    public Vector2 vel;
    public float r;
    public Vector3 color;

    public MetaBall(Vector2 pos, Vector2 vel, float r, Vector3 color){
        this.pos = pos;
        this.vel = vel;
        this.r = r;
        this.color = color;
    }
}
