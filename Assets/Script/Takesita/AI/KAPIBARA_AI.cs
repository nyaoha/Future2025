using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class KAPIBARA_AI : MonoBehaviour
{

    // --- AIState用変数
    public enum AIState { Idle, Walking, Eating } // AIState::AIの状態を定義
                                                  // Idle::待機
                                                  // Walking::歩く
                                                  // Eating::食べる

    public AIState currentState = AIState.Idle;   // 初期値を待機ステートに設定
    private bool switchAction = false;            // ステート切り替え用フラグ
    private float actionTimer = 0;                // 次の行動までの待機時間

    // --- AI移動に関する変数
    public float walkingSpeed = 3.5f;             // 歩くスピード
    public float walkingProbability = 0.5f;       // AIがランダムに歩く割合
    public float minRange = 3.0f;                 // 移動範囲の最小値
    public float maxRange = 7.0f;                 // 移動範囲の最大値
    public float minIdleTime = 0.1f;              // Idle状態の最小時間
    public float maxIdleTime = 2.0f;              // Idle状態の最大時間
    public float minEatingTime = 0.1f;            // Eating状態の最小時間
    public float maxEatingTime = 2.0f;            // Eating状態の最大時間
    public float rotationSpeed = 0.5f;            // 回転速度
    public float actionInterval = 5f;              // 目的地を設定する間隔

    public Animator animator;                     // Animotor用変数
    private NavMeshAgent agent;                    // ナビメッシュ用変数

    // 前回のIdleポイントを保持するためのリスト
    List<Vector3> previousIdlePoints = new List<Vector3>();
    private Vector3 currentDestination;            // 現在の目的地
    // 生成するPrefab
    private List<GameObject> spawnedObjects = new List<GameObject>(); // 生成したオブジェクトを保持するリスト
    public GameObject objectToSpawn;               // 生成するオブジェクトのPrefab

    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = 0;
        agent.autoBraking = true;
        agent.updateRotation = false;

        // AIの初期状態をWalkingに設定
        currentState = AIState.Walking;  // 初期ステートを強制的にWalkingに
        actionTimer = actionInterval;    // 最初の行動タイマーをリセット
        SwitchAnimationState(currentState); // アニメーションをWalkingに設定

        // 最初の目的地を設定
        SetNewDestination();
        // 最初のアニメーション設定（ここが不足している可能性）
        if (currentState == AIState.Walking)
        {
            animator.SetBool("isWalking", true); // 最初の移動時にアニメーションを設定
        }
        // 目的地にオブジェクトを生成
        SpawnObjectAtDestination(currentDestination);
    }

    // Update is called once per frame
    void Update()
    {
        // 次の行動まで待機
        if (actionTimer > 0)
        {
            actionTimer -= Time.deltaTime; // actionTimerを減少させる
        }
        else
        {
            switchAction = true; // タイマーが0以下になったらswitchActionをtrueにする
        }

        if (switchAction)
        {
            switch (currentState)
            {
                case AIState.Idle:
                    // ランダムに「食べる」か「歩く」行動を選択
                    if (Random.value > walkingProbability)
                    {
                        // 食べる
                        currentState = AIState.Eating;
                        actionTimer = Random.Range(minEatingTime, maxEatingTime);
                    }
                    else
                    {
                        // 歩く
                        SetNewDestination();
                        currentState = AIState.Walking;
                        actionTimer = actionInterval; // 次の行動までのタイマーをリセット
                    }
                    break;

                case AIState.Walking:
                    // 目的地に到達したかどうか確認
                    if (DoneReachingDestination())
                    {
                        currentState = AIState.Idle;
                        actionTimer = Random.Range(minIdleTime, maxIdleTime); // 次の行動までのタイマーをリセット
                    }
                    break;

                case AIState.Eating:
                    // Eating状態が終わったらIdleに戻る
                    currentState = AIState.Idle;
                    actionTimer = Random.Range(minIdleTime, maxIdleTime); // 次の行動までのタイマーをリセット
                    break;
            }

            switchAction = false; // 行動が完了したのでリセット
            SwitchAnimationState(currentState); // アニメーションの切り替え
        }

        // アニメーションの制御（エージェントの速度に基づく）
        if (agent.velocity.sqrMagnitude > 0.1f) // 移動中かどうか確認
        {
            animator.SetBool("isWalking", true);
        }
        else
        {
            animator.SetBool("isWalking", false);
        }

        // 回転の処理（歩行中の場合）
        if (currentState == AIState.Walking)
        {
            RotateTowardsDestination();
        }
    }

    void SetNewDestination()
    {
        currentDestination = RandomNavSphere(transform.position, Random.Range(minRange, maxRange));
        agent.destination = currentDestination;
        Debug.Log("New Destination Set: " + currentDestination); // デバッグ用
        actionTimer = actionInterval; // 次の目的地設定までのタイマーをリセット
    }

    bool DoneReachingDestination()
    {
        if (!agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    // 目的地に到達したらオブジェクトを消去
                    foreach (GameObject obj in spawnedObjects)
                    {
                        Destroy(obj); // オブジェクトを削除
                    }
                    spawnedObjects.Clear(); // リストをクリア
                    return true;
                }
            }
        }
        return false;
    }

    void SwitchAnimationState(AIState state)
    {
        // アニメーション制御
        if (animator)
        {
            // 他のステートを全てfalseにして、状態がリセットされることを保証
            animator.SetBool("isWalking", false);
            animator.SetBool("isEating", false);

            // 現在のステートに応じて該当するアニメーションをtrueに設定
            if (state == AIState.Walking)
            {
                animator.SetBool("isWalking", true);
            }
            else if (state == AIState.Eating)
            {
                animator.SetBool("isEating", true);
            }
        }
    }

    Vector3 RandomNavSphere(Vector3 origin, float distance)
    {
        Vector3 randomDirection = Random.insideUnitSphere * distance;
        randomDirection += origin;

        NavMeshHit navHit;
        NavMesh.SamplePosition(randomDirection, out navHit, distance, NavMesh.AllAreas);

        return navHit.position;
    }

    // 目的地にオブジェクトを生成
    void SpawnObjectAtDestination(Vector3 position)
    {
        if (objectToSpawn)
        {
            GameObject spawnedObject = Instantiate(objectToSpawn, position, Quaternion.identity); // 指定した位置にオブジェクトを生成
            spawnedObjects.Add(spawnedObject); // 生成したオブジェクトをリストに追加
            Debug.Log("Object spawned at: " + position); // デバッグ用
        }
        else
        {
            Debug.LogWarning("Object to spawn is not assigned!"); // 警告メッセージ
        }
    }

    void OnDrawGizmos()
    {
        // Gizmosを使って移動範囲を描画
        Gizmos.color = Color.green; // 緑色で描画
        Gizmos.DrawWireSphere(transform.position, maxRange); // 最大範囲を描画
        Gizmos.color = Color.yellow; // 黄色で描画
        Gizmos.DrawWireSphere(transform.position, minRange); // 最小範囲を描画

        // 現在の目的地を示す
        Gizmos.color = Color.red; // 赤色で描画
        Gizmos.DrawSphere(currentDestination, 0.2f); // 目的地のマーカーを描画

        // AIと目的地をつなぐラインを描画
        Gizmos.color = Color.blue; // 青色で描画
        Gizmos.DrawLine(transform.position, currentDestination); // AIから目的地までのラインを描画
    }

    void RotateTowardsDestination()
    {
        // エージェントの目的地までの方向を計算
        Vector3 direction = (agent.steeringTarget - transform.position).normalized;

        // 方向がゼロでないことを確認（エージェントが目的地に近づきすぎているとゼロになる可能性がある）
        if (direction != Vector3.zero)
        {
            // LookRotationを使って目的地の方向に回転させる
            Quaternion lookRotation = Quaternion.LookRotation(direction);

            // 現在の回転を新しい回転に補間（スムーズな回転を実現）
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }
}
