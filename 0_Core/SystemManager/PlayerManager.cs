using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class PlayerManager : MonoSingleton<PlayerManager>
{
    #region 配置
    //用于控制玩家的位置，装卸玩家预制件用的
    [Header("Player Management")]
    [SerializeField] private GameObject PlayerPrefab;
    [SerializeField] private Transform PlayerControlTransform;  //用于控制玩家位置并且挂载玩家预制件的Transform
    [SerializeField] private CinemachineVirtualCamera _camera;  //用于控制玩家位置并且挂载玩家预制件的Transform

    [SerializeField] private PolygonCollider2D TownCollider;
    [SerializeField] private PolygonCollider2D ChurchCollider;
    [SerializeField] private PolygonCollider2D GardenCollider;

    #region 运行时
    private GameObject playerInstance;
    /// <summary>玩家实例的 Transform（未实例化时为 null）。</summary>
    public Transform PlayerTransform => playerInstance != null ? playerInstance.transform : null;
    #endregion
    #endregion

    #region life
    protected override void Start()
    {
        base.Start();
        _camera.gameObject.GetComponent<CinemachineConfiner2D>().m_BoundingShape2D = TownCollider;
    }
    protected override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
        _camera.gameObject.GetComponent<CinemachineConfiner2D>().m_BoundingShape2D = TownCollider;
    }
    #endregion

    #region 冻结玩家移动和解冻玩家移动
    /// <summary>
    /// 冻结玩家对象，使玩家无法移动
    /// </summary>
    public void FreezePlayer()
    {
        InputManager.Instance?.EnableUIActionMap();   // 禁用 Player 输入 → MoveVector 变 0

        if (playerInstance != null)
        {
            var player = playerInstance.GetComponent<Player>();
            if (player != null) player.SetVelocity(0, 0);   // 立刻停住，避免滑动
        }
    }
    /// <summary>
    /// 解冻玩家对象，使玩家可以移动
    /// </summary>
    public void ThawPlayer()
    {
        Player player = playerInstance.GetComponent<Player>();
        if (player != null)
        {
            InputManager.Instance?.EnablePlayerActionMap();   // 启用 Player 输入
        }
        else
            Debug.LogError("Player instance is null or does not have a Player component.");
    }
    #endregion

    #region 激活玩家（添加玩家到场景中并且摄像机跟随玩家）
    public void testActive()
    {
        ActivatePlayerObject(new Vector3(0, 0, 0));
    }
    /// <summary>
    /// 激活玩家对象，将玩家添加到场景中，并且摄像机跟随玩家
    /// </summary>
    /// <param name="vector3"></param>
    public void ActivatePlayerObject(Vector3 vector3)
    {
        //将玩家设置到场景中，并且将摄像机的跟随目标设置为玩家
        InitPlayer(vector3);
        ResetCameraFollow(vector3);
        Debug.Log($"Player activated at position: {vector3}");
        PlayerDataManager.Instance?.SetLastLocation(Location.GetIdByPosition(vector3) ?? LocationId.Home);
    }
    private void InitPlayer(Vector3 vector3)
    {
        
        if(GameObject.FindGameObjectWithTag("Player") != null)
        {
            RemovePlayer();
        }

        //将玩家预制件实例化到场景中
        if (PlayerPrefab != null && PlayerControlTransform != null && playerInstance == null)
        {
            playerInstance = Instantiate(PlayerPrefab, vector3, Quaternion.identity, PlayerControlTransform);
            playerInstance.GetComponent<Player>().rb = playerInstance.GetComponentInParent<Rigidbody2D>();
            playerInstance.GetComponent<PlayerAnimController>().ReplaceAnimation(PlayerDataManager.Instance.GetAllPartIdentifiers());
        }
    }
    private void ResetCameraFollow(Vector3 vector3)
    {
        //将玩家设置到场景中，并且将摄像机的跟随目标设置为玩家
        if (_camera != null && PlayerControlTransform != null)
        {
            _camera.Follow = playerInstance.transform;
            _camera.gameObject.SetActive(true);
            PolygonCollider2D currentCollider = null;
            LocationId? lid = Location.GetIdByPosition(vector3);
            Debug.Log($"ResetCameraFollow: Player position {vector3}, LocationId: {lid}");
            if (lid == null)
            {
                // 不在任何已知位置
                currentCollider = TownCollider;
            }
            else
                switch (lid)
                {
                    case LocationId.Garden:
                        currentCollider = GardenCollider;
                        break;
                    case LocationId.Church:
                        currentCollider = ChurchCollider;
                        break;
                    default:
                        currentCollider = TownCollider;
                        break;
                }
            _camera.gameObject.GetComponent<CinemachineConfiner2D>().m_BoundingShape2D = currentCollider;
        }
    }
    #endregion

    #region 移动玩家
    /// <summary>
    /// 单纯的移动玩家实例对象，并不修改其他内容
    /// </summary>
    /// <param name="v"></param>
    public void MovePlayerTo(Vector3 v)
    {
        if (playerInstance != null)
        {
            playerInstance.transform.localPosition = v;
        }
        ResetCameraFollow(v);
        PlayerDataManager.Instance?.SetLastLocation(Location.GetIdByPosition(v) ?? LocationId.Home);
    }
    #endregion

    #region 移除玩家（从场景中移除玩家），并且摄像机不再跟随玩家，并且摄像机位置重置到默认位置
    /// <summary>
    /// 移除玩家对象，将玩家从场景中移除，并且摄像机不再跟随玩家，并且摄像机位置重置到默认位置
    /// </summary>
    public void UninstallPlayerObject()
    {
        //将玩家从场景中移除，并且将摄像机的跟随目标设置为null
        RemovePlayer();
        CloseCameraFollow();
    }

    private void RemovePlayer()
    {
        //将玩家从场景中移除
        if (PlayerControlTransform != null)
        {
            foreach (Transform child in PlayerControlTransform)
            {
                Destroy(child.gameObject);
            }
            playerInstance = null;
        }
    }
    private void CloseCameraFollow()
    {
        //将玩家从场景中移除，并且将摄像机的跟随目标设置为null
        if (_camera != null)
        {
            _camera.gameObject.GetComponent<CinemachineConfiner2D>().m_BoundingShape2D = TownCollider;
            _camera.Follow = null;
            _camera.transform.parent.position = new Vector3(3000, 3000, -10);  // Reset camera position to a default value、、
            _camera.gameObject.SetActive(false);
        }
    }
    #endregion
}
