using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MetaBallsHandler : MonoBehaviour
{
    [SerializeField] private RenderTexture renderTexture;
    [SerializeField] private ComputeShader metaBallsComputeShader;
    [SerializeField] private ComputeShader cursorComputeShader;
    [SerializeField] private ComputeShader clearScreenComputeShader;

    [Space]

    [Range(0f, 10f), SerializeField] private float surface;
    [SerializeField] private float newRadius;
    
    [SerializeField] private Vector2Int minAndMaxVel;

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
        metaBallsComputeShader.SetTexture(0, "Result", renderTexture);
        cursorComputeShader.SetTexture(0, "Result", renderTexture);
        clearScreenComputeShader.SetTexture(0, "Result", renderTexture);
        Cursor.visible = false;
    }
    private void Update() {
        SetNewRadius();
        SetNearestMetaBall();
        if (Input.GetKeyDown(KeyCode.R)) SetRandomVelocities();
        if (Input.GetKeyDown(KeyCode.Space)) transformBalls = !transformBalls;
        if (Input.GetMouseButtonDown(0)) SpawnMetaBall();
        if (nearestMetaBall != -1 && Input.GetMouseButtonDown(2)){
            metaBalls.RemoveAt(nearestMetaBall);
            nearestMetaBall = -1;
        }
        if (nearestMetaBall != -1 && !transformBalls && Input.GetMouseButton(1)){
            Vector2Int mousePos = GetCursorPos();
            MetaBall nearestBall = metaBalls[nearestMetaBall];
            metaBalls[nearestMetaBall] = new MetaBall(
             mousePos.x, mousePos.y,
             nearestBall.velX, nearestBall.velY,
             nearestBall.r);
        } 
        if (transformBalls) TransformBalls();
        SetupAndDispatchClearScreenComputeShader();
        if (metaBalls.Count > 0) SetupAndDispatchMetaBallsComputeShader();
        if (!Input.GetMouseButton(1))SetupAndDispatchCursorComputeShader();
    }
    private void SetupAndDispatchMetaBallsComputeShader(){
        metaBallsComputeShader.SetFloat("surface", surface);

        metaBallsComputeShader.SetInt("metaBallCount", metaBalls.Count);        
        ComputeBuffer metaBallsCB = new ComputeBuffer(metaBalls.Count, 4 * sizeof(int) + sizeof(float));
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
        metaBalls.Add(new MetaBall(
        mousePos.x, mousePos.y,
        Random.Range(minAndMaxVel.x, minAndMaxVel.y),
        Random.Range(minAndMaxVel.x, minAndMaxVel.y), newRadius));
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

    private void SetRandomVelocities(){
        for (int i = 0; i < metaBalls.Count; i++){
            MetaBall metaBall = metaBalls[i];
            metaBalls[i] = new MetaBall(
            metaBall.x, metaBall.y,
            Random.Range(minAndMaxVel.x, minAndMaxVel.y), Random.Range(minAndMaxVel.x, minAndMaxVel.y),
            metaBall.r);
        }
    }

    private void TransformBalls(){
        for (int i = 0; i < metaBalls.Count; i++){
            MetaBall metaBall = metaBalls[i];

            if (metaBall.x - metaBall.r <= 0 || metaBall.x + metaBall.r >= width){
                metaBall.velX *= -1;
            }
            if (metaBall.y - metaBall.r <= 0 || metaBall.y + metaBall.r >= height){
                metaBall.velY *= -1;
            }
            metaBall.x += metaBall.velX;
            metaBall.y += metaBall.velY;

            metaBalls[i] = new MetaBall(metaBall.x, metaBall.y, metaBall.velX, metaBall.velY, metaBall.r);
        }
    }
    
    private void OnRenderImage(RenderTexture src, RenderTexture dest) {
        Graphics.Blit(renderTexture, dest);
    }
}

struct MetaBall{
    public int x;
    public int y;

    public int velX;
    public int velY;

    public float r;

    public MetaBall(int x, int y, int velX, int velY, float r){
        this.x = x;
        this.y = y;
        this.velX = velX;
        this.velY = velY;
        this.r = r;
    }
}
