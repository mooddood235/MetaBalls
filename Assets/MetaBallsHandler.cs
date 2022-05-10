using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MetaBallsHandler : MonoBehaviour
{
    [SerializeField]private RenderTexture renderTexture;
    [SerializeField] private ComputeShader metaBallsComputeShader;
    [SerializeField] private ComputeShader cursorComputeShader;
    [SerializeField] private ComputeShader clearScreenComputeShader;

    [Space]

    [Range(0f, 10f), SerializeField] private float surface;
    [SerializeField] private float newRadius;

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
        metaBallsComputeShader.SetTexture(0, "Result", renderTexture);
        cursorComputeShader.SetTexture(0, "Result", renderTexture);
        clearScreenComputeShader.SetTexture(0, "Result", renderTexture);
        Cursor.visible = false;
    }
    private void Update() {
        SetNewRadius();
        SetNearestMetaBall();
        if (Input.GetMouseButtonDown(0)) SpawnMetaBall();
        if (nearestMetaBall != -1 && Input.GetMouseButtonDown(2)){
            metaBalls.RemoveAt(nearestMetaBall);
            nearestMetaBall = -1;
        }
        if (nearestMetaBall != -1 && Input.GetMouseButton(1)){
            Vector2Int mousePos = GetCursorPos();
            metaBalls[nearestMetaBall] = new MetaBall(mousePos.x, mousePos.y, metaBalls[nearestMetaBall].r);
        } 
        SetupAndDispatchClearScreenComputeShader();
        if (metaBalls.Count > 0) SetupAndDispatchMetaBallsComputeShader();
        if (!Input.GetMouseButton(1))SetupAndDispatchCursorComputeShader();
    }
    private void SetupAndDispatchMetaBallsComputeShader(){
        metaBallsComputeShader.SetFloat("surface", surface);

        metaBallsComputeShader.SetInt("metaBallCount", metaBalls.Count);        
        ComputeBuffer metaBallsCB = new ComputeBuffer(metaBalls.Count, 2 * sizeof(uint) + sizeof(float));
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
        Vector2Int mousePos = GetCursorPos();
        metaBalls.Add(new MetaBall(mousePos.x, mousePos.y, newRadius));
    }
    private void SetNearestMetaBall(){
        Vector2Int mousePos = GetCursorPos();
        int nearestBall = -1;
        float nearestDistance = Mathf.Infinity;

        for (int i = 0; i < metaBalls.Count; i++){
            MetaBall ball = metaBalls[i];
            float distance = Vector2Int.Distance(mousePos, new Vector2Int(ball.x, ball.y));
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
    private void SetNewRadius(){
        newRadius += Input.mouseScrollDelta.y;
    }
    private void OnRenderImage(RenderTexture src, RenderTexture dest) {
        Graphics.Blit(renderTexture, dest);
    }
}

struct MetaBall{
    public int x;
    public int y;
    public float r;

    public MetaBall(int x, int y, float r){
        this.x = x;
        this.y = y;
        this.r = r;
    }
}
