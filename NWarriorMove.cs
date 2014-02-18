using UnityEngine;
using System.Collections;

public class NWarriorMove : MonoBehaviour {

    public Camera cam;				// ссылка на нашу камеру
    public GameObject fireEffect;
    #region anim
    public float speed = 1f;
    public float gravity = 20f;

    public AnimationClip a_Idle;
    public float a_IdleSpeed = 1;

    public AnimationClip a_Walk;
    public float a_WalkSpeed = 1;

    public AnimationClip a_Kickh;
    public float a_KickhSpeed = 6;

    public AnimationClip a_Kickth;
    public float a_KickthSpeed = 6;

    public AnimationClip a_Kickf;
    public float a_KickfSpeed = 6;

    public AnimationClip a_HeadKick;
    public float a_HeadKickSpeed = 6;

    public AnimationClip a_BobyKick;
    public float a_BobyKickSpeed = 6;

    private string s_anim;

    private CharacterController controller;

    Vector3 moveDirection = Vector3.zero;
    #endregion

    private float lastSynchronizationTime = 0f;
    private float syncDelay = 0f;
    private float syncTime = 0f;
    private Vector3 syncStartPosition = Vector3.zero;
    private Vector3 syncEndPosition = Vector3.zero;
    private Quaternion rot;					// поворот 
    private int numCurAnim;					// номер анимации для сереализации 0 ожидание 1 ходьба 2 бег 3 прыжок 

    public int MyHealth = 100;
    public int EnemyHealth = 100;
    public int EnemyAnim = 0;
    // При создании объекта со скриптом
    void Awake()
    {
        cam = transform.GetComponentInChildren<Camera>().camera;
        
        controller = GetComponent<CharacterController>();

        animation[a_Idle.name].speed = a_IdleSpeed;
        animation[a_Walk.name].speed = a_WalkSpeed;
        animation[a_Kickh.name].speed = a_KickhSpeed;
        animation[a_Kickth.name].speed = a_KickthSpeed;
        animation[a_Kickf.name].speed = a_KickfSpeed;
        animation[a_HeadKick.name].speed = a_HeadKickSpeed;
        animation[a_BobyKick.name].speed = a_BobyKickSpeed;

        animation[a_Idle.name].wrapMode = WrapMode.Loop;
        animation[a_Walk.name].wrapMode = WrapMode.Loop;
        animation[a_Kickh.name].wrapMode = WrapMode.Loop;
        animation[a_Kickth.name].wrapMode = WrapMode.Loop;
        animation[a_Kickf.name].wrapMode = WrapMode.Loop;
        animation[a_HeadKick.name].wrapMode = WrapMode.Loop;
        animation[a_BobyKick.name].wrapMode = WrapMode.Loop;

        s_anim = a_Idle.name;
        numCurAnim = 0;
    }

    void Update()
    {
        if (networkView.isMine)
        {
            animation.CrossFade(s_anim);
            //Debug.Log(s_anim);
            if (controller.isGrounded)
            {
                moveDirection = new Vector3(0, 0, Input.GetAxis("Vertical"));
                moveDirection = transform.TransformDirection(moveDirection);
                moveDirection *= speed;

                // Анимация ходьбы
                if (Input.GetAxis("Vertical") > 0)
                {
                    s_anim = a_Walk.name;
                    animation[a_Walk.name].speed = a_WalkSpeed;
                    numCurAnim = 1; //идти
                }
                if (Input.GetAxis("Vertical") < 0)
                {
                    s_anim = a_Walk.name;
                    animation[a_Walk.name].speed = a_WalkSpeed * -1;
                    numCurAnim = 1; //идти
                }
               
                if (Input.GetAxis("Vertical") == 0)
                {
                    s_anim = a_Idle.name;
                    numCurAnim = 0; //ожидание
                }

                if (Input.GetKey(KeyCode.Space))
                {
                    s_anim = a_Kickf.name;
                    animation[a_Kickf.name].speed = a_KickfSpeed;
                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        Atack();
                    }
                    numCurAnim = 2; //удар ногой
                }

                if (Input.GetKey(KeyCode.E))
                {
                    s_anim = a_Kickh.name;
                    animation[a_Kickh.name].speed = a_KickhSpeed;
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        Atack();
                    }
                    numCurAnim = 3; //удар рукой
                }

                if (Input.GetKey(KeyCode.Q))
                {
                    s_anim = a_Kickth.name;
                    animation[a_Kickth.name].speed = a_KickthSpeed;
                    if (Input.GetKeyDown(KeyCode.Q))
                    {
                        Atack();
                    }
                    numCurAnim = 4; //удар двумя руками
                }
            }
            
            moveDirection.y -= gravity * Time.deltaTime;
            controller.Move(moveDirection * Time.deltaTime);
            transform.Rotate(Vector3.down * speed * Input.GetAxis("Horizontal") * -1, Space.World);
        }
        else
        {
            if (cam.enabled)
            {
                cam.enabled = false;
                cam.gameObject.GetComponent<AudioListener>().enabled = false;
            }
            SyncedMovement();
        }
    }

    int w = 0;
    int l = 0;
    private void OnGUI()
    {
        if (networkView.isMine)
        {
            GUI.Label(new Rect(10, 40, 100, 20), "Enemy Hp: " + EnemyHealth.ToString());
            if (EnemyHealth <= 0)
            {
                w++;
                EnemyHealth = 100;
                if (Network.isServer)
                {
                    transform.position = new Vector3(-3, .5f, -3);
                }
                else
                {
                    transform.position = new Vector3(3, .5f, -3);
                }
            }
            GUI.Label(new Rect(10, 60, 100, 20), "Win: " + w.ToString());
            GUI.Label(new Rect(10, 100, 100, 20), "anim: " + EnemyAnim.ToString());
            
        }
        else
        {
            GUI.Label(new Rect(10, 20, 100, 20), "My Hp: " + MyHealth.ToString());
            GUI.Label(new Rect(10, 80, 100, 20), "Lost: " + l.ToString());
            
        }
        
    }

    // Интерполяция
    private void SyncedMovement()
    {
        syncTime += Time.deltaTime;
        transform.position = Vector3.Lerp(syncStartPosition, syncEndPosition, syncTime / syncDelay);
    }

    // Вызывается с определенной частотой. Отвечает за сереализицию переменных
    void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
    {
        int synHealth = 0;
        Vector3 syncPosition = Vector3.zero;
        int sWin = 0;
        
        if (stream.isWriting)
        {
            rot = transform.rotation;
            syncPosition = transform.position;
            synHealth = EnemyHealth;
            sWin = w;

            stream.Serialize(ref syncPosition);
            stream.Serialize(ref rot);
            stream.Serialize(ref numCurAnim);
            stream.Serialize(ref synHealth);
            stream.Serialize(ref sWin);
        }
        else
        {
            stream.Serialize(ref syncPosition);
            stream.Serialize(ref rot);
            stream.Serialize(ref numCurAnim);
            stream.Serialize(ref synHealth);
            stream.Serialize(ref sWin);
            l = sWin;

            PlayNameAnim(); 
            

            MyHealth = synHealth;

            animation.CrossFade(s_anim);

            transform.rotation = rot;

            // Расчеты для интерполяции

            // Находим время между текущим моментом и последней интерполяцией
            syncTime = 0f;
            syncDelay = Time.time - lastSynchronizationTime;
            lastSynchronizationTime = Time.time;

            syncStartPosition = transform.position;
            syncEndPosition = syncPosition;
            //Debug.Log(networkView.viewID + " " + syncStartPosition + " " + syncEndPosition);
        }
    }

    // Определение анимации по номеру
    public void PlayNameAnim()
    {
        switch (numCurAnim)
        {
            case 0:
                s_anim = a_Idle.name;
                break;
            case 1:
                s_anim = a_Walk.name;
                break;
            case 2:
                s_anim = a_Kickf.name;
                break;
            case 3:
                s_anim = a_Kickh.name;
                break;
            case 4:
                s_anim = a_Kickth.name;
                break;
        }
    }

    void Atack()
    {
        if (networkView.isMine)
        {
            Vector3 DirectionRay = transform.TransformDirection(Vector3.forward);
            DirectionRay.y = 0.5f;
            RaycastHit Hit;
            if (Physics.Raycast(this.transform.position, DirectionRay, out Hit, 1.5f))
            {
                //Debug.Log("numAnim=" + s_anim + " dist=" + Mydistans);
                if (Hit.distance != 0 & Hit.distance < 2f)
                {
                    float ys=1.6f;
                    switch (s_anim)
                    {
                        // w_kickh, w_kickth, w_kickf
                        case "w_kickh":
                            if (Hit.distance < 1.5f)
                                EnemyHealth--;
                            break;
                        case "w_kickth":
                            if (Hit.distance < 1.5f)
                                EnemyHealth = EnemyHealth - 5;
                            break;
                        case "w_kickf":
                            ys=1f;
                            EnemyHealth = EnemyHealth - 3;
                            break;
                    }
                    GameObject kick = Instantiate(fireEffect,new Vector3(Hit.point.x,ys,Hit.point.z), Quaternion.identity) as GameObject;
                    Destroy(kick, .5f);
                }
            }
        }
    }

}
