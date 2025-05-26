# Gravity Flip Lab - 重力反転・物理システム詳細ドキュメント

## 目次

1. [システム概要](#システム概要)
2. [アーキテクチャ設計](#アーキテクチャ設計)
3. [データ構造](#データ構造)
4. [コアシステム詳細](#コアシステム詳細)
5. [クラス仕様](#クラス仕様)
6. [API リファレンス](#api-リファレンス)
7. [実装ガイド](#実装ガイド)
8. [ベストプラクティス](#ベストプラクティス)
9. [トラブルシューティング](#トラブルシューティング)
10. [パフォーマンス最適化](#パフォーマンス最適化)

---

## システム概要

Gravity Flip Lab の重力反転・物理システムは、ゲームの核となる重力制御メカニクスを実現する統合物理システムです。このシステムは、グローバル重力制御、局所重力ゾーン、慣性システム、そして特殊物理効果を一元管理し、滑らかで直感的な重力反転体験を提供します。

### 主要特徴

- **瞬間重力反転**: 0.1秒での瞬時重力方向切り替え
- **局所重力ゾーン**: エリア限定の特殊重力効果
- **慣性システム**: 物理的に自然な重力変更時の挙動
- **スプリング・斜面物理**: 環境との相互作用
- **軌道予測**: 重力変更時の物体軌道計算
- **パフォーマンス最適化**: 大量オブジェクトでの効率的な物理演算

### 物理仕様

- **重力強度**: 9.81 units/sec² (Earth gravity)
- **反転時間**: 0.1秒の滑らかな遷移
- **最大重力**: 50.0 units/sec² (制限値)
- **慣性保持**: 重力変更時80%保持
- **局所重力範囲**: 半径1-20 units (設定可能)

---

## アーキテクチャ設計

### システム構成図

```
┌─────────────────────────────────────────────┐
│              GravitySystem                  │
│           (中央重力制御システム)             │
├─────────────────────────────────────────────┤
│ ┌─────────────────┐ ┌─────────────────────┐ │
│ │ LocalGravityZone│ │ GravityAffectedObject│ │
│ │ (局所重力ゾーン) │ │ (重力影響オブジェクト)│ │
│ └─────────────────┘ └─────────────────────┘ │
│ ┌─────────────────┐ ┌─────────────────────┐ │
│ │ MomentumController│ │ GravityPhysicsUtils │ │
│ │ (慣性制御)       │ │ (物理計算ユーティリティ)│ │
│ └─────────────────┘ └─────────────────────┘ │
├─────────────────────────────────────────────┤
│ ┌─────────────────┐ ┌─────────────────────┐ │
│ │ SpringPlatform  │ │ SlopePhysics        │ │
│ │ (スプリング)     │ │ (斜面物理)          │ │
│ └─────────────────┘ └─────────────────────┘ │
└─────────────────────────────────────────────┘
```

### 設計原則

1. **Physics First**: Unity物理エンジンとの完全統合
2. **Modular Zones**: 独立した局所重力ゾーン管理
3. **Smooth Transitions**: 滑らかな重力遷移とアニメーション
4. **Performance Optimized**: 大規模シーンでの効率的な処理
5. **Predictable Behavior**: 一貫性のある物理挙動

### 依存関係図

```
GravitySystem (Core)
    ├── Unity Physics2D (Engine)
    ├── LocalGravityZone (Zones)
    │   ├── GravityWell (Specialized)
    │   └── WindTunnel (Environmental)
    ├── GravityAffectedObject (Objects)
    │   └── MomentumController (Physics)
    └── GravityPhysicsUtils (Calculations)
        ├── SpringPlatform (Interactive)
        └── SlopePhysics (Environmental)
```

---

## データ構造

### GravitySettings（重力設定データ）

重力システムの基本設定を定義します。

```csharp
[System.Serializable]
public class GravitySettings
{
    [Header("Global Gravity")]
    public float globalGravityStrength = 9.81f;        // グローバル重力強度
    public Vector2 globalGravityDirection = Vector2.down; // グローバル重力方向
    
    [Header("Flip Settings")]
    public float flipTransitionTime = 0.1f;            // 重力反転遷移時間
    public AnimationCurve flipCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 反転カーブ
    
    [Header("Physics")]
    public float maxGravityForce = 50f;                // 最大重力値
    public float gravityAcceleration = 2f;             // 重力加速度係数
    public bool useRealisticPhysics = true;            // リアル物理挙動使用
}
```

**フィールド詳細**:
- `globalGravityStrength`: 基準となる重力の強さ（地球重力ベース）
- `globalGravityDirection`: グローバル重力の方向ベクトル
- `flipTransitionTime`: 重力反転にかかる時間（秒）
- `flipCurve`: 重力反転時の補間カーブ
- `maxGravityForce`: 重力値の上限（異常値防止）
- `gravityAcceleration`: 重力変更時の加速度係数
- `useRealisticPhysics`: 物理的に正確な計算を使用するか

### LocalGravityType（局所重力タイプ）

局所重力ゾーンの重力タイプを定義します。

```csharp
public enum LocalGravityType
{
    Directional,    // 固定方向重力（通常重力の方向違い）
    Radial,         // 放射状重力（中心点への引力/斥力）  
    Orbital,        // 軌道重力（中心点周りの回転力）
    Custom          // カスタム重力関数
}
```

**タイプ詳細**:
- `Directional`: 一定方向への重力（上下左右など）
- `Radial`: 中心点に向かう/から離れる放射状の力
- `Orbital`: 中心点周りの円軌道を作る力
- `Custom`: プログラマーが独自に定義する重力関数

### LocalGravityData（局所重力データ）

局所重力ゾーンの設定データです。

```csharp
[System.Serializable]
public class LocalGravityData
{
    [Header("Gravity Type")]
    public LocalGravityType gravityType = LocalGravityType.Directional; // 重力タイプ
    public Vector2 direction = Vector2.down;               // 重力方向（Directional用）
    public float strength = 9.81f;                         // 重力強度
    
    [Header("Zone Behavior")]
    public bool overrideGlobal = true;                     // グローバル重力を上書き
    public float transitionDistance = 1f;                 // 遷移距離
    public AnimationCurve strengthCurve = AnimationCurve.Linear(0, 0, 1, 1); // 強度カーブ
}
```

**フィールド詳細**:
- `gravityType`: 重力の種類（方向性、放射状、軌道、カスタム）
- `direction`: Directional重力での方向ベクトル
- `strength`: 重力の強さ（units/sec²）
- `overrideGlobal`: グローバル重力を完全に置き換えるか
- `transitionDistance`: ゾーン境界での遷移範囲
- `strengthCurve`: 中心からの距離による強度変化カーブ

---

## コアシステム詳細

### GravitySystem（重力システム中核）

ゲーム全体の重力を統括管理するシングルトンシステムです。

#### 主要責任

1. **グローバル重力管理**: ゲーム全体の基準重力制御
2. **重力反転実行**: 瞬間的な重力方向切り替え
3. **局所ゾーン統合**: 局所重力ゾーンとの連携
4. **物理エンジン制御**: Unity Physics2Dとの同期
5. **イベント配信**: 重力変更イベントの発行

#### 重力反転メカニズム

```csharp
public void FlipGlobalGravity()
{
    Vector2 newDirection = -CurrentGravityDirection;
    SetGlobalGravityDirection(newDirection);
}

private IEnumerator GravityTransitionCoroutine(Vector2 targetDirection)
{
    Vector2 startDirection = CurrentGravityDirection;
    float elapsedTime = 0f;

    while (elapsedTime < settings.flipTransitionTime)
    {
        float t = elapsedTime / settings.flipTransitionTime;
        float curveValue = settings.flipCurve.Evaluate(t);
        
        // 滑らかな方向補間
        Vector2 currentDirection = Vector2.Lerp(startDirection, targetDirection, curveValue);
        CurrentGravityDirection = currentDirection.normalized;
        
        // Unity物理エンジンに適用
        UnityEngine.Physics2D.gravity = CurrentGravityDirection * CurrentGravityStrength;
        
        // イベント発行
        OnGlobalGravityChanged?.Invoke(CurrentGravityDirection);
        
        elapsedTime += Time.deltaTime;
        yield return null;
    }

    // 最終値の確定
    CurrentGravityDirection = targetDirection;
    UnityEngine.Physics2D.gravity = CurrentGravityDirection * CurrentGravityStrength;
    OnGlobalGravityChanged?.Invoke(CurrentGravityDirection);
}
```

#### 位置ベース重力計算

```csharp
public Vector2 GetGravityAtPosition(Vector3 position)
{
    // 局所重力ゾーンのチェック
    foreach (var zone in gravityZones)
    {
        if (zone != null && zone.IsPositionInZone(position))
        {
            return zone.GetGravityVector();
        }
    }
    
    // グローバル重力を返す
    return CurrentGravityDirection * CurrentGravityStrength;
}
```

### LocalGravityZone（局所重力ゾーン）

特定エリア内で独自の重力を適用するシステムです。

#### ゾーン形状管理

```csharp
public bool IsPositionInZone(Vector3 position)
{
    if (useColliderBounds && zoneCollider != null)
    {
        return zoneCollider.bounds.Contains(position);
    }
    else
    {
        Vector2 distance = position - transform.position;
        return distance.magnitude <= radius;
    }
}
```

#### 重力タイプ別計算

```csharp
public virtual Vector2 GetGravityAtPosition(Vector3 position)
{
    Vector2 gravityVector = Vector2.zero;

    switch (gravityData.gravityType)
    {
        case LocalGravityType.Directional:
            gravityVector = gravityData.direction.normalized * gravityData.strength;
            break;

        case LocalGravityType.Radial:
            Vector2 directionToCenter = (transform.position - position).normalized;
            gravityVector = directionToCenter * gravityData.strength;
            break;

        case LocalGravityType.Orbital:
            Vector2 toCenter = transform.position - position;
            Vector2 perpendicular = new Vector2(-toCenter.y, toCenter.x).normalized;
            gravityVector = perpendicular * gravityData.strength;
            break;

        case LocalGravityType.Custom:
            gravityVector = CalculateCustomGravity(position);
            break;
    }

    // 距離による強度調整
    float distance = Vector2.Distance(position, transform.position);
    float normalizedDistance = Mathf.Clamp01(distance / gravityData.transitionDistance);
    float strengthMultiplier = gravityData.strengthCurve.Evaluate(1f - normalizedDistance);
    
    return gravityVector * strengthMultiplier;
}
```

### GravityAffectedObject（重力影響オブジェクト）

重力システムの影響を受けるオブジェクトの制御システムです。

#### カスタム重力適用

```csharp
private void ApplyGravity()
{
    if (rb2d == null) return;

    // Unity自動重力を無効化
    rb2d.gravityScale = 0f;

    // カスタム重力適用
    Vector2 gravityForce = targetGravity * gravityScale;
    
    // 速度変化の制限
    Vector2 velocityChange = gravityForce * Time.fixedDeltaTime;
    if (velocityChange.magnitude > maxVelocityChange)
    {
        velocityChange = velocityChange.normalized * maxVelocityChange;
    }

    if (maintainInertia)
    {
        // 慣性を保持しながら重力適用
        Vector2 newVelocity = rb2d.velocity + velocityChange;
        rb2d.velocity = Vector2.Lerp(rb2d.velocity, newVelocity, inertiaDecay);
    }
    else
    {
        // 直接重力適用
        rb2d.AddForce(gravityForce, ForceMode2D.Force);
    }

    currentGravity = targetGravity;
}
```

#### 重力ゾーン遷移管理

```csharp
public void EnterGravityZone(LocalGravityZone zone)
{
    if (!activeZones.Contains(zone))
    {
        activeZones.Add(zone);
        currentZone = zone;
    }
}

public void ExitGravityZone(LocalGravityZone zone)
{
    activeZones.Remove(zone);
    
    if (currentZone == zone)
    {
        currentZone = activeZones.Count > 0 ? activeZones[activeZones.Count - 1] : null;
    }
}

private Vector2 CalculateEffectiveGravity()
{
    if (activeZones.Count == 0)
    {
        // グローバル重力使用
        return GravitySystem.Instance.GetGravityAtPosition(transform.position);
    }

    // 最新ゾーン（最高優先度）を使用
    LocalGravityZone priorityZone = activeZones[activeZones.Count - 1];
    return priorityZone.GetGravityAtPosition(transform.position);
}
```

---

## 特殊重力ゾーン

### GravityWell（重力井戸）

中心点への引力または斥力を生成する特殊ゾーンです。

```csharp
public class GravityWell : LocalGravityZone
{
    [Header("Gravity Well Settings")]
    public float wellStrength = 15f;           // 井戸の強度
    public float maxRadius = 10f;              // 最大影響半径
    public bool repulsive = false;             // 斥力モード
    public AnimationCurve falloffCurve = AnimationCurve.EaseInOut(0, 1, 1, 0); // 減衰カーブ

    public override Vector2 GetGravityAtPosition(Vector3 position)
    {
        Vector2 directionToCenter = (transform.position - position);
        float distance = directionToCenter.magnitude;
        
        if (distance > maxRadius) return Vector2.zero;
        
        // 減衰カーブ適用
        float normalizedDistance = distance / maxRadius;
        float strength = wellStrength * falloffCurve.Evaluate(normalizedDistance);
        
        // 斥力の場合は方向を反転
        Vector2 direction = repulsive ? -directionToCenter.normalized : directionToCenter.normalized;
        
        return direction * strength;
    }
}
```

**使用例**:
- **ブラックホール効果**: 強力な引力で物体を吸い込む
- **反重力フィールド**: 物体を押し出す斥力場
- **軌道制御**: 惑星のような引力による軌道運動

### WindTunnel（風のトンネル）

一定方向への継続的な力を与える環境効果です。

```csharp
public class WindTunnel : MonoBehaviour
{
    [Header("Wind Settings")]
    public Vector2 windDirection = Vector2.right;  // 風の方向
    public float windStrength = 5f;                // 風の強さ
    public float turbulence = 0f;                  // 乱流強度
    public float gustFrequency = 2f;               // 突風頻度

    private void ApplyWindForce()
    {
        foreach (var rb in affectedObjects)
        {
            if (rb == null) continue;

            Vector2 windForce = CalculateWindForce();
            rb.AddForce(windForce, ForceMode2D.Force);
        }
    }

    private Vector2 CalculateWindForce()
    {
        Vector2 baseWind = windDirection.normalized * windStrength;
        
        // 乱流と突風の追加
        if (turbulence > 0f)
        {
            float turbulenceX = Mathf.PerlinNoise(Time.time * gustFrequency, 0f) * 2f - 1f;
            float turbulenceY = Mathf.PerlinNoise(0f, Time.time * gustFrequency) * 2f - 1f;
            Vector2 turbulenceForce = new Vector2(turbulenceX, turbulenceY) * turbulence;
            
            baseWind += turbulenceForce;
        }

        return baseWind;
    }
}
```

**効果**:
- **水平移動アシスト**: プレイヤーの移動を補助
- **環境的挑戦**: 風に逆らった移動の難易度
- **動的環境**: 時間変化する風向きと強度

---

## 慣性・モーメンタムシステム

### MomentumController（慣性制御）

重力変更時の物理的に自然な慣性挙動を管理します。

```csharp
public class MomentumController : MonoBehaviour
{
    [Header("Momentum Settings")]
    public float momentumDecay = 0.95f;            // 慣性減衰率
    public float maxMomentum = 15f;                // 最大慣性値
    public bool preserveMomentumOnGravityFlip = true; // 重力反転時慣性保持
    
    [Header("Inertia")]
    public float inertiaStrength = 1f;             // 慣性強度
    public float velocitySmoothing = 0.1f;         // 速度スムージング

    private void OnGravityFlip(GravityFlipLab.Player.GravityDirection direction)
    {
        if (preserveMomentumOnGravityFlip)
        {
            // 慣性保持処理
            Vector2 velocity = rb2d.velocity;
            momentum.x = velocity.x * 0.8f;      // 水平慣性80%保持
            momentum.y = -velocity.y * 0.5f;     // 垂直慣性反転・50%保持
        }
    }

    private void UpdateMomentum()
    {
        // 速度変化による慣性計算
        Vector2 velocityChange = rb2d.velocity - previousVelocity;
        momentum += velocityChange * inertiaStrength;
        
        // 減衰適用
        momentum *= momentumDecay;
        
        // 慣性上限制限
        if (momentum.magnitude > maxMomentum)
        {
            momentum = momentum.normalized * maxMomentum;
        }
    }
}
```

**物理的効果**:
- **重力反転時の滑らかな挙動**: 急激な速度変化を防止
- **現実的な慣性**: 物理法則に沿った自然な動き
- **プレイヤビリティ**: 予測可能で制御しやすい挙動

---

## 環境相互作用システム

### SpringPlatform（スプリングプラットフォーム）

物体に反発力を与える弾性プラットフォームです。

```csharp
public class SpringPlatform : MonoBehaviour
{
    [Header("Spring Settings")]
    public float springForce = 15f;                // スプリング力
    public float springDamping = 0.8f;             // 減衰係数
    public float compressionDistance = 0.5f;       // 圧縮距離
    public AnimationCurve springCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // スプリングカーブ

    private void ApplySpringForce(Rigidbody2D rb, Collision2D collision)
    {
        // 衝突法線によるスプリング方向計算
        Vector2 springDirection = collision.contacts[0].normal;
        
        // スプリング力適用
        Vector2 force = -springDirection * springForce;
        rb.AddForce(force, ForceMode2D.Impulse);
        
        // 減衰適用（無限バウンド防止）
        rb.velocity *= springDamping;
        
        // スプリングアニメーション再生
        PlaySpringAnimation();
    }

    private IEnumerator SpringAnimationCoroutine()
    {
        isCompressed = true;
        float elapsedTime = 0f;
        
        while (elapsedTime < animationDuration)
        {
            float t = elapsedTime / animationDuration;
            float curveValue = springCurve.Evaluate(t);
            
            Vector3 offset = Vector3.down * compressionDistance * (1f - curveValue);
            springVisual.localPosition = originalPosition + offset;
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        springVisual.localPosition = originalPosition;
        isCompressed = false;
    }
}
```

### SlopePhysics（斜面物理）

斜面での特殊な物理挙動を処理します。

```csharp
public class SlopePhysics : MonoBehaviour
{
    [Header("Slope Settings")]
    public float slopeSpeedMultiplier = 1.2f;      // 斜面速度倍率
    public float maxSlopeAngle = 45f;              // 最大斜面角度
    public bool affectGravity = true;              // 重力影響有無
    public float gravityRedirection = 0.5f;        // 重力方向変更強度

    private void ApplySlopePhysics(Rigidbody2D rb, ContactPoint2D contact)
    {
        Vector2 normal = contact.normal;
        float slopeAngle = Vector2.Angle(normal, Vector2.up);
        
        if (slopeAngle > maxSlopeAngle) return;

        // 斜面方向計算
        Vector2 slopeDirection = Vector2.Perpendicular(normal);
        if (slopeDirection.x < 0) slopeDirection = -slopeDirection;

        // 斜面速度補正
        Vector2 velocity = rb.velocity;
        float slopeInfluence = Mathf.Clamp01(slopeAngle / maxSlopeAngle);
        velocity.x *= 1f + (slopeSpeedMultiplier - 1f) * slopeInfluence;

        // 斜面に沿った重力方向変更
        if (affectGravity)
        {
            Vector2 gravityRedirect = Vector3.Project(Physics2D.gravity, slopeDirection);
            rb.AddForce(gravityRedirect * gravityRedirection, ForceMode2D.Force);
        }

        rb.velocity = velocity;
    }
}
```

---

## 物理計算ユーティリティ

### GravityPhysicsUtils（重力物理計算）

重力関連の数学的計算を提供する静的ユーティリティクラスです。

```csharp
public static class GravityPhysicsUtils
{
    /// <summary>
    /// 軌道計算 - 指定時間後の位置を予測
    /// </summary>
    public static Vector2 CalculateTrajectory(Vector2 startPos, Vector2 startVelocity, Vector2 gravity, float time)
    {
        // s = ut + (1/2)at² の物理公式
        return startPos + startVelocity * time + 0.5f * gravity * time * time;
    }

    /// <summary>
    /// 速度計算 - 指定時間後の速度を予測
    /// </summary>
    public static Vector2 CalculateVelocityAtTime(Vector2 startVelocity, Vector2 gravity, float time)
    {
        // v = u + at の物理公式
        return startVelocity + gravity * time;
    }

    /// <summary>
    /// 高度到達時間計算
    /// </summary>
    public static float CalculateTimeToReachHeight(Vector2 startVelocity, Vector2 gravity, float targetHeight)
    {
        // 二次方程式を使用: s = ut + (1/2)at²
        float a = 0.5f * gravity.y;
        float b = startVelocity.y;
        float c = -targetHeight;

        float discriminant = b * b - 4 * a * c;
        if (discriminant < 0) return -1f; // 解が存在しない

        float time1 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
        float time2 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);

        // 正の時間を返す
        return Mathf.Max(time1, time2);
    }

    /// <summary>
    /// 軌道座標配列計算 - 軌道の可視化用
    /// </summary>
    public static Vector2[] CalculateTrajectoryPoints(Vector2 startPos, Vector2 startVelocity, Vector2 gravity, int pointCount, float timeStep)
    {
        Vector2[] points = new Vector2[pointCount];
        
        for (int i = 0; i < pointCount; i++)
        {
            float time = i * timeStep;
            points[i] = CalculateTrajectory(startPos, startVelocity, gravity, time);
        }

        return points;
    }

    /// <summary>
    /// 地面衝突判定
    /// </summary>
    public static bool WillCollideWithGround(Vector2 startPos, Vector2 startVelocity, Vector2 gravity, float groundY, out float timeToCollision)
    {
        timeToCollision = CalculateTimeToReachHeight(startVelocity, gravity, groundY - startPos.y);
        return timeToCollision > 0f;
    }
}
```

**使用例**:
```csharp
// プレイヤーの軌道予測
Vector2 playerPos = playerTransform.position;
Vector2 playerVel = playerRigidbody.velocity;
Vector2 currentGravity = GravitySystem.Instance.CurrentGravityDirection * GravitySystem.Instance.CurrentGravityStrength;

// 2秒後の位置を予測
Vector2 futurePosition = GravityPhysicsUtils.CalculateTrajectory(playerPos, playerVel, currentGravity, 2f);

// 軌道可視化用の点群を生成
Vector2[] trajectoryPoints = GravityPhysicsUtils.CalculateTrajectoryPoints(playerPos, playerVel, currentGravity, 50, 0.1f);
```

---

## クラス仕様

### GravitySystem

**継承**: MonoBehaviour  
**パターン**: Singleton  
**DontDestroyOnLoad**: 有効

#### プロパティ

| プロパティ名 | 型 | アクセス | 説明 |
|-------------|----|---------|----- |
| CurrentGravityDirection | Vector2 | public | 現在の重力方向 |
| CurrentGravityStrength | float | public | 現在の重力強度 |

#### イベント

| イベント名 | 型 | 説明 |
|-----------|----|----- |
| OnGlobalGravityChanged | System.Action\<Vector2\> | グローバル重力変更時 |
| OnGravityStrengthChanged | System.Action\<float\> | 重力強度変更時 |

#### メソッド

| メソッド名 | 戻り値 | 説明 |
|-----------|--------|----- |
| FlipGlobalGravity() | void | グローバル重力反転 |
| SetGlobalGravityDirection(Vector2) | void | 重力方向設定 |
| SetGravityStrength(float) | void | 重力強度設定 |
| GetGravityAtPosition(Vector3) | Vector2 | 位置別重力取得 |
| RegisterGravityZone(LocalGravityZone) | void | 重力ゾーン登録 |

### LocalGravityZone

**継承**: MonoBehaviour

#### フィールド

| フィールド名 | 型 | デフォルト値 | 説明 |
|-------------|----|-----------|----- |
| gravityData | LocalGravityData | - | 重力設定データ |
| radius | float | 5f | ゾーン半径 |
| useColliderBounds | bool | true | コライダー境界使用 |
| showGizmos | bool | true | ギズモ表示 |

#### メソッド

| メソッド名 | 戻り値 | 説明 |
|-----------|--------|----- |
| IsPositionInZone(Vector3) | bool | 位置がゾーン内か判定 |
| GetGravityVector() | Vector2 | 重力ベクトル取得 |
| GetGravityAtPosition(Vector3) | Vector2 | 位置別重力計算 |

### GravityAffectedObject

**継承**: MonoBehaviour  
**必要コンポーネント**: Rigidbody2D

#### フィールド

| フィールド名 | 型 | デフォルト値 | 説明 |
|-------------|----|-----------|----- |
| gravityScale | float | 1f | 重力スケール |
| useCustomGravity | bool | true | カスタム重力使用 |
| smoothGravityTransition | bool | true | 滑らかな重力遷移 |
| maxVelocityChange | float | 20f | 最大速度変化 |

#### メソッド

| メソッド名 | 戻り値 | 説明 |
|-----------|--------|----- |
| EnterGravityZone(LocalGravityZone) | void | 重力ゾーン進入 |
| ExitGravityZone(LocalGravityZone) | void | 重力ゾーン退出 |
| GetCurrentGravity() | Vector2 | 現在重力取得 |
| SetGravityScale(float) | void | 重力スケール設定 |

---

## API リファレンス

### GravitySystem API

#### インスタンスアクセス

```csharp
GravitySystem.Instance
```

#### 重力制御

```csharp
// グローバル重力反転
GravitySystem.Instance.FlipGlobalGravity();

// 重力方向設定
GravitySystem.Instance.SetGlobalGravityDirection(Vector2.up);

// 重力強度設定
GravitySystem.Instance.SetGravityStrength(15f);

// 現在重力情報取得
Vector2 direction = GravitySystem.Instance.CurrentGravityDirection;
float strength = GravitySystem.Instance.CurrentGravityStrength;

// 位置別重力計算
Vector2 gravity = GravitySystem.Instance.GetGravityAtPosition(transform.position);
```

#### イベント購読

```csharp
private void OnEnable()
{
    GravitySystem.OnGlobalGravityChanged += OnGravityChanged;
    GravitySystem.OnGravityStrengthChanged += OnStrengthChanged;
}

private void OnDisable()
{
    GravitySystem.OnGlobalGravityChanged -= OnGravityChanged;
    GravitySystem.OnGravityStrengthChanged -= OnStrengthChanged;
}

private void OnGravityChanged(Vector2 newDirection)
{
    Debug.Log($"Gravity changed to: {newDirection}");
    // 環境エフェクト、UI更新など
}

private void OnStrengthChanged(float newStrength)
{
    Debug.Log($"Gravity strength: {newStrength}");
}
```

### LocalGravityZone API

#### ゾーン設定

```csharp
// 重力ゾーンの設定
LocalGravityZone zone = GetComponent<LocalGravityZone>();

// 方向性重力設定
zone.gravityData.gravityType = LocalGravityType.Directional;
zone.gravityData.direction = Vector2.left;
zone.gravityData.strength = 12f;

// 放射状重力設定
zone.gravityData.gravityType = LocalGravityType.Radial;
zone.gravityData.strength = 20f;

// 軌道重力設定
zone.gravityData.gravityType = LocalGravityType.Orbital;
zone.gravityData.strength = 8f;
```

#### カスタム重力実装

```csharp
public class CustomGravityZone : LocalGravityZone
{
    public float waveAmplitude = 5f;
    public float waveFrequency = 1f;
    
    protected override Vector2 CalculateCustomGravity(Vector3 position)
    {
        // 波状の重力場を作成
        float wave = Mathf.Sin(Time.time * waveFrequency) * waveAmplitude;
        Vector2 waveDirection = new Vector2(wave, -9.81f);
        return waveDirection;
    }
}
```

### GravityPhysicsUtils API

#### 軌道計算

```csharp
// 基本軌道計算
Vector2 startPos = transform.position;
Vector2 startVel = rigidbody2D.velocity;
Vector2 gravity = Physics2D.gravity;

// 3秒後の位置予測
Vector2 futurePos = GravityPhysicsUtils.CalculateTrajectory(startPos, startVel, gravity, 3f);

// 軌道可視化
Vector2[] trajectory = GravityPhysicsUtils.CalculateTrajectoryPoints(
    startPos, startVel, gravity, 100, 0.1f);

// LineRendererで軌道描画
LineRenderer lineRenderer = GetComponent<LineRenderer>();
lineRenderer.positionCount = trajectory.Length;
for (int i = 0; i < trajectory.Length; i++)
{
    lineRenderer.SetPosition(i, trajectory[i]);
}
```

#### 衝突予測

```csharp
// 地面との衝突時間計算
float groundY = 0f;
float timeToImpact;

if (GravityPhysicsUtils.WillCollideWithGround(startPos, startVel, gravity, groundY, out timeToImpact))
{
    Debug.Log($"Ground impact in {timeToImpact:F2} seconds");
    
    // 衝突位置予測
    Vector2 impactPos = GravityPhysicsUtils.CalculateTrajectory(startPos, startVel, gravity, timeToImpact);
    
    // エフェクト事前準備など
    PrepareImpactEffect(impactPos, timeToImpact);
}
```

---

## 実装ガイド

### 基本セットアップ

#### 1. GravitySystemの設置

```csharp
// 空のGameObjectを作成し、GravitySystemスクリプトをアタッチ
GameObject gravitySystemObj = new GameObject("GravitySystem");
GravitySystem gravitySystem = gravitySystemObj.AddComponent<GravitySystem>();

// 設定の調整
gravitySystem.settings.globalGravityStrength = 9.81f;
gravitySystem.settings.flipTransitionTime = 0.1f;
gravitySystem.settings.maxGravityForce = 50f;
```

#### 2. プレイヤーへの重力影響設定

```csharp
// プレイヤーオブジェクトにGravityAffectedObjectを追加
GameObject player = GameObject.FindGameObjectWithTag("Player");
GravityAffectedObject gravityAffected = player.AddComponent<GravityAffectedObject>();

// 重力設定
gravityAffected.gravityScale = 1f;
gravityAffected.useCustomGravity = true;
gravityAffected.smoothGravityTransition = true;
gravityAffected.transitionSpeed = 5f;
```

#### 3. 局所重力ゾーンの作成

```csharp
// 重力ゾーンオブジェクト作成
GameObject gravityZone = new GameObject("GravityZone");
LocalGravityZone zone = gravityZone.AddComponent<LocalGravityZone>();

// トリガーコライダー設定
CircleCollider2D trigger = gravityZone.AddComponent<CircleCollider2D>();
trigger.isTrigger = true;
trigger.radius = 5f;

// 重力設定
zone.gravityData.gravityType = LocalGravityType.Directional;
zone.gravityData.direction = Vector2.up;
zone.gravityData.strength = 9.81f;
zone.gravityData.overrideGlobal = true;
```

### プレイヤー重力反転の実装

```csharp
public class PlayerGravityController : MonoBehaviour
{
    private GravityAffectedObject gravityAffected;
    
    private void Start()
    {
        gravityAffected = GetComponent<GravityAffectedObject>();
    }
    
    private void Update()
    {
        // スペースキーで重力反転
        if (Input.GetKeyDown(KeyCode.Space))
        {
            FlipGravity();
        }
    }
    
    private void FlipGravity()
    {
        // グローバル重力を反転
        GravitySystem.Instance.FlipGlobalGravity();
        
        // プレイヤー固有の処理
        StartCoroutine(GravityFlipEffect());
    }
    
    private IEnumerator GravityFlipEffect()
    {
        // 視覚エフェクト
        PlayGravityFlipParticles();
        
        // 音響エフェクト
        // AudioSource.PlayOneShot(gravityFlipSound);
        
        // 短時間の無敵
        gravityAffected.enabled = false;
        yield return new WaitForSeconds(0.1f);
        gravityAffected.enabled = true;
    }
    
    private void PlayGravityFlipParticles()
    {
        // パーティクルシステム再生
        ParticleSystem particles = GetComponent<ParticleSystem>();
        if (particles != null)
        {
            particles.Play();
        }
    }
}
```

### 重力井戸の設置

```csharp
public class GravityWellSetup : MonoBehaviour
{
    [Header("Well Configuration")]
    public float wellRadius = 10f;
    public float wellStrength = 15f;
    public bool isRepulsive = false;
    
    private void Start()
    {
        SetupGravityWell();
    }
    
    private void SetupGravityWell()
    {
        // GravityWellコンポーネント追加
        GravityWell gravityWell = gameObject.AddComponent<GravityWell>();
        
        // 設定適用
        gravityWell.maxRadius = wellRadius;
        gravityWell.wellStrength = wellStrength;
        gravityWell.repulsive = isRepulsive;
        
        // 視覚表現の作成
        CreateVisualEffect();
        
        // コライダー設定
        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<CircleCollider2D>();
        }
        collider.isTrigger = true;
        collider.radius = wellRadius;
    }
    
    private void CreateVisualEffect()
    {
        // パーティクルシステムで視覚効果作成
        GameObject effectObj = new GameObject("WellEffect");
        effectObj.transform.SetParent(transform);
        effectObj.transform.localPosition = Vector3.zero;
        
        ParticleSystem particles = effectObj.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.startLifetime = 2f;
        main.startSpeed = 5f;
        main.maxParticles = 100;
        
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = wellRadius;
    }
}
```

### 動的重力場の実装

```csharp
public class DynamicGravityField : LocalGravityZone
{
    [Header("Dynamic Settings")]
    public float rotationSpeed = 30f;          // 重力方向回転速度
    public float strengthVariation = 0.5f;     // 強度変動幅
    public float variationFrequency = 1f;      // 変動周波数
    
    private Vector2 baseDirection;
    private float baseStrength;
    
    protected override void Start()
    {
        base.Start();
        baseDirection = gravityData.direction;
        baseStrength = gravityData.strength;
    }
    
    private void Update()
    {
        UpdateDynamicGravity();
    }
    
    private void UpdateDynamicGravity()
    {
        // 重力方向の回転
        float angle = Time.time * rotationSpeed;
        gravityData.direction = new Vector2(
            Mathf.Cos(angle * Mathf.Deg2Rad),
            Mathf.Sin(angle * Mathf.Deg2Rad)
        );
        
        // 重力強度の変動
        float strengthMod = Mathf.Sin(Time.time * variationFrequency) * strengthVariation;
        gravityData.strength = baseStrength + strengthMod;
        
        // 視覚エフェクトの更新
        UpdateVisualEffect();
    }
    
    private void UpdateVisualEffect()
    {
        // パーティクルの方向を重力方向に合わせる
        ParticleSystem particles = GetComponentInChildren<ParticleSystem>();
        if (particles != null)
        {
            var velocityOverLifetime = particles.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.x = gravityData.direction.x * gravityData.strength;
            velocityOverLifetime.y = gravityData.direction.y * gravityData.strength;
        }
    }
}
```

---

## ベストプラクティス

### 重力システムの最適化

#### 1. 重力計算の効率化

```csharp
public class OptimizedGravityCalculation : MonoBehaviour
{
    private static readonly WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();
    private Dictionary<Transform, Vector2> gravityCache = new Dictionary<Transform, Vector2>();
    private float cacheUpdateInterval = 0.1f;
    private float lastCacheUpdate = 0f;
    
    public Vector2 GetOptimizedGravity(Transform target)
    {
        // キャッシュを使用した重力計算
        if (Time.time - lastCacheUpdate > cacheUpdateInterval)
        {
            UpdateGravityCache();
            lastCacheUpdate = Time.time;
        }
        
        if (gravityCache.ContainsKey(target))
        {
            return gravityCache[target];
        }
        
        return GravitySystem.Instance.GetGravityAtPosition(target.position);
    }
    
    private void UpdateGravityCache()
    {
        gravityCache.Clear();
        
        // アクティブなオブジェクトの重力を一括計算
        GravityAffectedObject[] objects = FindObjectsOfType<GravityAffectedObject>();
        foreach (var obj in objects)
        {
            if (obj.enabled && obj.gameObject.activeInHierarchy)
            {
                Vector2 gravity = GravitySystem.Instance.GetGravityAtPosition(obj.transform.position);
                gravityCache[obj.transform] = gravity;
            }
        }
    }
}
```

#### 2. 重力ゾーンの階層管理

```csharp
public class GravityZoneManager : MonoBehaviour
{
    private static GravityZoneManager _instance;
    public static GravityZoneManager Instance => _instance;
    
    [Header("Zone Management")]
    public int maxActiveZones = 10;
    
    private List<LocalGravityZone> allZones = new List<LocalGravityZone>();
    private Dictionary<Transform, List<LocalGravityZone>> objectZoneMap = new Dictionary<Transform, List<LocalGravityZone>>();
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void RegisterZone(LocalGravityZone zone)
    {
        if (!allZones.Contains(zone))
        {
            allZones.Add(zone);
            OptimizeZoneList();
        }
    }
    
    public void UnregisterZone(LocalGravityZone zone)
    {
        allZones.Remove(zone);
        
        // オブジェクトマップからも削除
        foreach (var kvp in objectZoneMap.ToList())
        {
            kvp.Value.Remove(zone);
            if (kvp.Value.Count == 0)
            {
                objectZoneMap.Remove(kvp.Key);
            }
        }
    }
    
    private void OptimizeZoneList()
    {
        // 距離ベースでゾーンを最適化
        if (allZones.Count > maxActiveZones)
        {
            Vector3 playerPos = GameObject.FindGameObjectWithTag("Player")?.transform.position ?? Vector3.zero;
            
            allZones.Sort((a, b) => 
            {
                float distA = Vector3.Distance(a.transform.position, playerPos);
                float distB = Vector3.Distance(b.transform.position, playerPos);
                return distA.CompareTo(distB);
            });
            
            // 遠いゾーンを一時的に無効化
            for (int i = maxActiveZones; i < allZones.Count; i++)
            {
                allZones[i].enabled = false;
            }
        }
    }
}
```

### エラーハンドリング

#### 1. 重力値の異常検出

```csharp
public class GravityValidator : MonoBehaviour
{
    [Header("Validation Settings")]
    public float maxReasonableGravity = 100f;
    public float minReasonableGravity = 0.1f;
    
    private void LateUpdate()
    {
        ValidateGravityValues();
    }
    
    private void ValidateGravityValues()
    {
        // グローバル重力の検証
        float currentStrength = GravitySystem.Instance.CurrentGravityStrength;
        if (float.IsNaN(currentStrength) || float.IsInfinity(currentStrength))
        {
            Debug.LogError("Invalid gravity strength detected, resetting to default");
            GravitySystem.Instance.SetGravityStrength(9.81f);
        }
        else if (currentStrength > maxReasonableGravity)
        {
            Debug.LogWarning($"Excessive gravity strength: {currentStrength}, clamping to {maxReasonableGravity}");
            GravitySystem.Instance.SetGravityStrength(maxReasonableGravity);
        }
        else if (currentStrength < minReasonableGravity)
        {
            Debug.LogWarning($"Too low gravity strength: {currentStrength}, setting to minimum {minReasonableGravity}");
            GravitySystem.Instance.SetGravityStrength(minReasonableGravity);
        }
        
        // 重力方向の検証
        Vector2 direction = GravitySystem.Instance.CurrentGravityDirection;
        if (float.IsNaN(direction.x) || float.IsNaN(direction.y) || 
            float.IsInfinity(direction.x) || float.IsInfinity(direction.y))
        {
            Debug.LogError("Invalid gravity direction detected, resetting to down");
            GravitySystem.Instance.SetGlobalGravityDirection(Vector2.down);
        }
    }
}
```

#### 2. 物理オブジェクトの安全性確保

```csharp
public class PhysicsObjectSafety : MonoBehaviour
{
    [Header("Safety Settings")]
    public float maxSafeVelocity = 50f;
    public float maxSafePosition = 1000f;
    public float resetPositionY = 0f;
    
    private GravityAffectedObject gravityObject;
    private Rigidbody2D rb2d;
    private Vector3 lastSafePosition;
    
    private void Start()
    {
        gravityObject = GetComponent<GravityAffectedObject>();
        rb2d = GetComponent<Rigidbody2D>();
        lastSafePosition = transform.position;
    }
    
    private void FixedUpdate()
    {
        CheckPhysicsIntegrity();
        UpdateSafePosition();
    }
    
    private void CheckPhysicsIntegrity()
    {
        // 速度の検証
        if (rb2d.velocity.magnitude > maxSafeVelocity)
        {
            Debug.LogWarning($"Excessive velocity detected: {rb2d.velocity.magnitude}, clamping");
            rb2d.velocity = rb2d.velocity.normalized * maxSafeVelocity;
        }
        
        // 位置の検証
        if (Mathf.Abs(transform.position.x) > maxSafePosition || 
            Mathf.Abs(transform.position.y) > maxSafePosition)
        {
            Debug.LogWarning("Object position out of bounds, teleporting to safe position");
            transform.position = lastSafePosition;
            rb2d.velocity = Vector2.zero;
        }
        
        // NaN/Infinity チェック
        if (float.IsNaN(transform.position.x) || float.IsNaN(transform.position.y) ||
            float.IsInfinity(transform.position.x) || float.IsInfinity(transform.position.y))
        {
            Debug.LogError("Invalid position detected, resetting to safe position");
            transform.position = new Vector3(0, resetPositionY, 0);
            rb2d.velocity = Vector2.zero;
        }
    }
    
    private void UpdateSafePosition()
    {
        // 安全な位置の更新（地面にいるとき）
        if (gravityObject != null && IsGroundedAndStable())
        {
            lastSafePosition = transform.position;
        }
    }
    
    private bool IsGroundedAndStable()
    {
        // 地面との接触と安定性をチェック
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1f);
        return hit.collider != null && rb2d.velocity.magnitude < 2f;
    }
}
```

---

## トラブルシューティング

### よくある問題と解決策

#### 1. 重力反転が正常に動作しない

**症状**: 重力反転メソッドを呼んでも物体の動きが変わらない

**原因**:
- Unity Physics2D.gravityが正しく設定されていない
- GravityAffectedObjectが正しく初期化されていない
- 重力スケールが0に設定されている

**解決策**:
```csharp
public class GravityFlipDebugger : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            DebugGravityState();
        }
        
        if (Input.GetKeyDown(KeyCode.F2))
        {
            ForceGravityFlip();
        }
    }
    
    private void DebugGravityState()
    {
        Debug.Log("=== Gravity System Debug ===");
        Debug.Log($"Unity Physics2D.gravity: {Physics2D.gravity}");
        Debug.Log($"GravitySystem Direction: {GravitySystem.Instance.CurrentGravityDirection}");
        Debug.Log($"GravitySystem Strength: {GravitySystem.Instance.CurrentGravityStrength}");
        
        // 重力影響オブジェクトの状態確認
        GravityAffectedObject[] objects = FindObjectsOfType<GravityAffectedObject>();
        Debug.Log($"Active GravityAffectedObjects: {objects.Length}");
        
        foreach (var obj in objects)
        {
            Debug.Log($"Object: {obj.name}, Custom Gravity: {obj.useCustomGravity}, Scale: {obj.gravityScale}");
        }
    }
    
    private void ForceGravityFlip()
    {
        Debug.Log("Forcing gravity flip...");
        
        // 直接Unity物理系に設定
        Physics2D.gravity = -Physics2D.gravity;
        
        // GravitySystemの状態も更新
        GravitySystem.Instance.SetGlobalGravityDirection(-GravitySystem.Instance.CurrentGravityDirection);
        
        Debug.Log($"New gravity: {Physics2D.gravity}");
    }
}
```

#### 2. 局所重力ゾーンが機能しない

**症状**: 重力ゾーン内に入っても重力が変化しない

**原因**:
- コライダーがトリガーに設定されていない
- レイヤーマスクの設定ミス
- オブジェクトにGravityAffectedObjectがアタッチされていない

**解決策**:
```csharp
public class GravityZoneValidator : MonoBehaviour
{
    [ContextMenu("Validate Gravity Zone Setup")]
    public void ValidateSetup()
    {
        List<string> issues = new List<string>();
        
        // LocalGravityZoneの確認
        LocalGravityZone zone = GetComponent<LocalGravityZone>();
        if (zone == null)
        {
            issues.Add("LocalGravityZone component not found");
        }
        else
        {
            if (zone.gravityData.strength <= 0)
            {
                issues.Add("Gravity strength is zero or negative");
            }
        }
        
        // コライダーの確認
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            issues.Add("Collider2D component not found");
        }
        else if (!col.isTrigger)
        {
            issues.Add("Collider is not set as trigger");
        }
        
        // GravitySystemの確認
        if (GravitySystem.Instance == null)
        {
            issues.Add("GravitySystem instance not found in scene");
        }
        
        // 結果出力
        if (issues.Count == 0)
        {
            Debug.Log($"✓ Gravity zone '{name}' setup is valid");
        }
        else
        {
            Debug.LogError($"✗ Gravity zone '{name}' has issues:\n{string.Join("\n", issues)}");
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Trigger Enter: {other.name}");
        
        GravityAffectedObject gravityObj = other.GetComponent<GravityAffectedObject>();
        if (gravityObj == null)
        {
            Debug.LogWarning($"Object {other.name} entered gravity zone but has no GravityAffectedObject component");
        }
    }
}
```

#### 3. パフォーマンスの低下

**症状**: 重力システム使用時にフレームレートが大幅に低下

**原因**:
- 過剰な物理計算
- 重力ゾーンの数が多すぎる
- FixedUpdateでの重い処理

**解決策**:
```csharp
public class GravityPerformanceMonitor : MonoBehaviour
{
    [Header("Performance Monitoring")]
    public bool enableProfiling = true;
    public float profilingInterval = 1f;
    
    private float lastProfilingTime = 0f;
    private int gravityCalculationsPerSecond = 0;
    private int frameCount = 0;
    
    private void Update()
    {
        if (!enableProfiling) return;
        
        frameCount++;
        
        if (Time.time - lastProfilingTime >= profilingInterval)
        {
            float fps = frameCount / profilingInterval;
            
            Debug.Log($"=== Gravity Performance Report ===");
            Debug.Log($"FPS: {fps:F1}");
            Debug.Log($"Gravity Calculations/sec: {gravityCalculationsPerSecond}");
            Debug.Log($"Active Gravity Zones: {FindObjectsOfType<LocalGravityZone>().Length}");
            Debug.Log($"Active Gravity Objects: {FindObjectsOfType<GravityAffectedObject>().Length}");
            
            // パフォーマンス警告
            if (fps < 30f)
            {
                Debug.LogWarning("Low FPS detected! Consider optimizing gravity system");
                SuggestOptimizations();
            }
            
            // リセット
            frameCount = 0;
            gravityCalculationsPerSecond = 0;
            lastProfilingTime = Time.time;
        }
    }
    
    public void IncrementGravityCalculation()
    {
        gravityCalculationsPerSecond++;
    }
    
    private void SuggestOptimizations()
    {
        List<string> suggestions = new List<string>();
        
        int zoneCount = FindObjectsOfType<LocalGravityZone>().Length;
        if (zoneCount > 10)
        {
            suggestions.Add($"Consider reducing gravity zones ({zoneCount} active)");
        }
        
        int objectCount = FindObjectsOfType<GravityAffectedObject>().Length;
        if (objectCount > 50)
        {
            suggestions.Add($"Consider object pooling for gravity objects ({objectCount} active)");
        }
        
        if (gravityCalculationsPerSecond > 1000)
        {
            suggestions.Add("Consider caching gravity calculations");
        }
        
        if (suggestions.Count > 0)
        {
            Debug.LogWarning("Optimization suggestions:\n" + string.Join("\n", suggestions));
        }
    }
}
```

#### 4. 重力遷移の不自然な動き

**症状**: 重力変更時にオブジェクトがテレポートしたり、不自然な動きをする

**原因**:
- 遷移時間が短すぎる
- 慣性の計算が正しくない
- アニメーションカーブが適切でない

**解決策**:
```csharp
public class SmoothGravityTransition : MonoBehaviour
{
    [Header("Transition Settings")]
    public float transitionDuration = 0.2f;
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool preserveVelocityMagnitude = true;
    
    private GravityAffectedObject gravityObject;
    private Rigidbody2D rb2d;
    private Coroutine transitionCoroutine;
    
    private void Start()
    {
        gravityObject = GetComponent<GravityAffectedObject>();
        rb2d = GetComponent<Rigidbody2D>();
        
        // 重力変更イベントに購読
        GravitySystem.OnGlobalGravityChanged += OnGravityChanged;
    }
    
    private void OnDestroy()
    {
        GravitySystem.OnGlobalGravityChanged -= OnGravityChanged;
    }
    
    private void OnGravityChanged(Vector2 newGravityDirection)
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        
        transitionCoroutine = StartCoroutine(SmoothTransitionCoroutine(newGravityDirection));
    }
    
    private IEnumerator SmoothTransitionCoroutine(Vector2 targetGravity)
    {
        Vector2 initialVelocity = rb2d.velocity;
        float initialSpeed = initialVelocity.magnitude;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < transitionDuration)
        {
            float t = elapsedTime / transitionDuration;
            float curveValue = transitionCurve.Evaluate(t);
            
            if (preserveVelocityMagnitude && initialSpeed > 0.1f)
            {
                // 速度の大きさを保ちながら方向を変更
                Vector2 targetDirection = targetGravity.normalized;
                Vector2 currentDirection = Vector2.Lerp(initialVelocity.normalized, targetDirection, curveValue);
                rb2d.velocity = currentDirection * initialSpeed * (1f - curveValue * 0.3f);
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        transitionCoroutine = null;
    }
}
```

---

## パフォーマンス最適化

### CPU最適化

#### 1. 重力計算の最適化

```csharp
public class OptimizedGravitySystem : MonoBehaviour
{
    [Header("Optimization Settings")]
    public int maxCalculationsPerFrame = 10;
    public float cacheValidDuration = 0.1f;
    
    private Dictionary<int, CachedGravityData> gravityCache = new Dictionary<int, CachedGravityData>();
    private Queue<GravityCalculationRequest> calculationQueue = new Queue<GravityCalculationRequest>();
    private int calculationsThisFrame = 0;
    
    private struct CachedGravityData
    {
        public Vector2 gravity;
        public float timestamp;
        public Vector3 position;
    }
    
    private struct GravityCalculationRequest
    {
        public Transform target;
        public System.Action<Vector2> callback;
    }
    
    private void Update()
    {
        calculationsThisFrame = 0;
        ProcessGravityCalculationQueue();
    }
    
    public void RequestGravityCalculation(Transform target, System.Action<Vector2> callback)
    {
        int targetId = target.GetInstanceID();
        
        // キャッシュチェック
        if (gravityCache.ContainsKey(targetId))
        {
            CachedGravityData cached = gravityCache[targetId];
            
            // キャッシュが有効かつ位置が近い場合
            if (Time.time - cached.timestamp < cacheValidDuration &&
                Vector3.Distance(cached.position, target.position) < 1f)
            {
                callback?.Invoke(cached.gravity);
                return;
            }
        }
        
        // 計算キューに追加
        calculationQueue.Enqueue(new GravityCalculationRequest
        {
            target = target,
            callback = callback
        });
    }
    
    private void ProcessGravityCalculationQueue()
    {
        while (calculationQueue.Count > 0 && calculationsThisFrame < maxCalculationsPerFrame)
        {
            GravityCalculationRequest request = calculationQueue.Dequeue();
            
            if (request.target == null) continue;
            
            // 重力計算実行
            Vector2 gravity = GravitySystem.Instance.GetGravityAtPosition(request.target.position);
            
            // キャッシュ更新
            int targetId = request.target.GetInstanceID();
            gravityCache[targetId] = new CachedGravityData
            {
                gravity = gravity,
                timestamp = Time.time,
                position = request.target.position
            };
            
            // コールバック実行
            request.callback?.Invoke(gravity);
            
            calculationsThisFrame++;
        }
    }
    
    public void ClearCache()
    {
        gravityCache.Clear();
    }
}
```

#### 2. 空間分割による最適化

```csharp
public class SpatialGravityGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    public float cellSize = 10f;
    public Vector2 gridBounds = new Vector2(100f, 100f);
    
    private Dictionary<Vector2Int, List<LocalGravityZone>> gravityGrid = new Dictionary<Vector2Int, List<LocalGravityZone>>();
    private Dictionary<Vector2Int, Vector2> cachedGravityValues = new Dictionary<Vector2Int, Vector2>();
    
    private void Start()
    {
        BuildGravityGrid();
        StartCoroutine(UpdateGridCache());
    }
    
    private void BuildGravityGrid()
    {
        gravityGrid.Clear();
        
        LocalGravityZone[] allZones = FindObjectsOfType<LocalGravityZone>();
        
        foreach (var zone in allZones)
        {
            Vector2Int gridPos = WorldToGrid(zone.transform.position);
            
            if (!gravityGrid.ContainsKey(gridPos))
            {
                gravityGrid[gridPos] = new List<LocalGravityZone>();
            }
            
            gravityGrid[gridPos].Add(zone);
        }
    }
    
    public Vector2 GetOptimizedGravity(Vector3 worldPosition)
    {
        Vector2Int gridPos = WorldToGrid(worldPosition);
        
        // キャッシュされた値があれば使用
        if (cachedGravityValues.ContainsKey(gridPos))
        {
            return cachedGravityValues[gridPos];
        }
        
        // グリッドセル内の重力ゾーンをチェック
        Vector2 resultGravity = GravitySystem.Instance.CurrentGravityDirection * GravitySystem.Instance.CurrentGravityStrength;
        
        if (gravityGrid.ContainsKey(gridPos))
        {
            foreach (var zone in gravityGrid[gridPos])
            {
                if (zone != null && zone.IsPositionInZone(worldPosition))
                {
                    resultGravivity = zone.GetGravityAtPosition(worldPosition);
                    break;
                }
            }
        }
        
        return resultGravity;
    }
    
    private Vector2Int WorldToGrid(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / cellSize),
            Mathf.FloorToInt(worldPos.y / cellSize)
        );
    }
    
    private IEnumerator UpdateGridCache()
    {
        while (true)
        {
            // グリッドキャッシュを定期的に更新
            yield return new WaitForSeconds(0.5f);
            
            cachedGravityValues.Clear();
            
            // 使用頻度の高いグリッドセルのみ事前計算
            foreach (var kvp in gravityGrid)
            {
                Vector3 worldPos = new Vector3(kvp.Key.x * cellSize, kvp.Key.y * cellSize, 0);
                Vector2 gravity = CalculateGravityForCell(worldPos, kvp.Value);
                cachedGravityValues[kvp.Key] = gravity;
                
                yield return null; // フレーム分散
            }
        }
    }
    
    private Vector2 CalculateGravityForCell(Vector3 cellCenter, List<LocalGravityZone> zones)
    {
        Vector2 gravity = GravitySystem.Instance.CurrentGravityDirection * GravitySystem.Instance.CurrentGravityStrength;
        
        foreach (var zone in zones)
        {
            if (zone != null && zone.IsPositionInZone(cellCenter))
            {
                gravity = zone.GetGravityAtPosition(cellCenter);
                break;
            }
        }
        
        return gravity;
    }
}
```

### メモリ最適化

#### 1. オブジェクトプーリング

```csharp
public class GravityEffectPool : MonoBehaviour
{
    [Header("Pool Settings")]
    public GameObject gravityEffectPrefab;
    public int poolSize = 20;
    
    private Queue<GameObject> effectPool = new Queue<GameObject>();
    private List<GameObject> activeEffects = new List<GameObject>();
    
    private void Start()
    {
        InitializePool();
    }
    
    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject effect = Instantiate(gravityEffectPrefab);
            effect.SetActive(false);
            effectPool.Enqueue(effect);
        }
    }
    
    public GameObject SpawnGravityEffect(Vector3 position, Vector2 gravityDirection)
    {
        GameObject effect;
        
        if (effectPool.Count > 0)
        {
            effect = effectPool.Dequeue();
        }
        else
        {
            effect = Instantiate(gravityEffectPrefab);
        }
        
        effect.transform.position = position;
        effect.SetActive(true);
        
        // エフェクトの方向設定
        GravityEffectController controller = effect.GetComponent<GravityEffectController>();
        if (controller != null)
        {
            controller.SetGravityDirection(gravityDirection);
        }
        
        activeEffects.Add(effect);
        
        // 自動回収のスケジュール
        StartCoroutine(ReturnToPoolAfterDelay(effect, 2f));
        
        return effect;
    }
    
    private IEnumerator ReturnToPoolAfterDelay(GameObject effect, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool(effect);
    }
    
    public void ReturnToPool(GameObject effect)
    {
        if (activeEffects.Contains(effect))
        {
            activeEffects.Remove(effect);
            effect.SetActive(false);
            effectPool.Enqueue(effect);
        }
    }
    
    public void ReturnAllToPool()
    {
        foreach (var effect in activeEffects.ToArray())
        {
            ReturnToPool(effect);
        }
    }
}
```

#### 2. メモリ使用量監視

```csharp
public class GravityMemoryMonitor : MonoBehaviour
{
    [Header("Memory Monitoring")]
    public bool enableMonitoring = true;
    public float monitoringInterval = 5f;
    public long memoryWarningThreshold = 100 * 1024 * 1024; // 100MB
    
    private void Start()
    {
        if (enableMonitoring)
        {
            InvokeRepeating(nameof(MonitorMemoryUsage), 0f, monitoringInterval);
        }
    }
    
    private void MonitorMemoryUsage()
    {
        long totalMemory = System.GC.GetTotalMemory(false);
        
        Debug.Log($"Gravity System Memory Usage: {totalMemory / 1024 / 1024} MB");
        
        if (totalMemory > memoryWarningThreshold)
        {
            Debug.LogWarning("High memory usage detected in gravity system");
            PerformMemoryCleanup();
        }
        
        // 詳細メモリ情報
        LogDetailedMemoryInfo();
    }
    
    private void LogDetailedMemoryInfo()
    {
        int gravityObjects = FindObjectsOfType<GravityAffectedObject>().Length;
        int gravityZones = FindObjectsOfType<LocalGravityZone>().Length;
        int gravityWells = FindObjectsOfType<GravityWell>().Length;
        int windTunnels = FindObjectsOfType<WindTunnel>().Length;
        
        Debug.Log($"Active Objects - Gravity Objects: {gravityObjects}, " +
                 $"Gravity Zones: {gravityZones}, Wells: {gravityWells}, Wind Tunnels: {windTunnels}");
    }
    
    private void PerformMemoryCleanup()
    {
        // 強制ガベージコレクション
        System.GC.Collect();
        
        // 不要なリソースをアンロード
        Resources.UnloadUnusedAssets();
        
        // エフェクトプールをクリア
        GravityEffectPool[] pools = FindObjectsOfType<GravityEffectPool>();
        foreach (var pool in pools)
        {
            pool.ReturnAllToPool();
        }
        
        Debug.Log("Gravity system memory cleanup completed");
    }
}
```

---

## 高度な機能と拡張

### 重力波システム

```csharp
public class GravityWaveSystem : MonoBehaviour
{
    [Header("Wave Settings")]
    public float waveSpeed = 10f;
    public float waveStrength = 5f;
    public float waveRadius = 20f;
    public AnimationCurve waveDecayCurve = AnimationCurve.EaseOut(0, 1, 1, 0);
    
    private struct GravityWave
    {
        public Vector3 center;
        public float currentRadius;
        public float strength;
        public float startTime;
        public bool isActive;
    }
    
    private List<GravityWave> activeWaves = new List<GravityWave>();
    
    public void CreateGravityWave(Vector3 center, float strength = -1f)
    {
        if (strength < 0) strength = waveStrength;
        
        GravityWave newWave = new GravityWave
        {
            center = center,
            currentRadius = 0f,
            strength = strength,
            startTime = Time.time,
            isActive = true
        };
        
        activeWaves.Add(newWave);
        
        // 視覚エフェクト
        CreateWaveVisualEffect(center, strength);
    }
    
    private void Update()
    {
        UpdateGravityWaves();
    }
    
    private void UpdateGravityWaves()
    {
        for (int i = activeWaves.Count - 1; i >= 0; i--)
        {
            GravityWave wave = activeWaves[i];
            
            if (!wave.isActive) continue;
            
            // 波の拡散
            float elapsedTime = Time.time - wave.startTime;
            wave.currentRadius = elapsedTime * waveSpeed;
            
            // 範囲外になったら削除
            if (wave.currentRadius > waveRadius)
            {
                activeWaves.RemoveAt(i);
                continue;
            }
            
            // 重力影響オブジェクトに波の効果を適用
            ApplyWaveEffect(wave);
            
            activeWaves[i] = wave;
        }
    }
    
    private void ApplyWaveEffect(GravityWave wave)
    {
        GravityAffectedObject[] objects = FindObjectsOfType<GravityAffectedObject>();
        
        foreach (var obj in objects)
        {
            float distance = Vector3.Distance(obj.transform.position, wave.center);
            
            // 波の範囲内かチェック
            if (distance <= wave.currentRadius + 1f && distance >= wave.currentRadius - 1f)
            {
                // 減衰計算
                float normalizedDistance = wave.currentRadius / waveRadius;
                float decayMultiplier = waveDecayCurve.Evaluate(normalizedDistance);
                
                // 波の方向
                Vector2 waveDirection = (obj.transform.position - wave.center).normalized;
                Vector2 waveForce = waveDirection * wave.strength * decayMultiplier;
                
                // 力を適用
                Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.AddForce(waveForce, ForceMode2D.Impulse);
                }
            }
        }
    }
    
    private void CreateWaveVisualEffect(Vector3 center, float strength)
    {
        // パーティクルシステムで波エフェクト作成
        GameObject waveEffect = new GameObject("GravityWave");
        waveEffect.transform.position = center;
        
        ParticleSystem particles = waveEffect.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.startLifetime = waveRadius / waveSpeed;
        main.startSpeed = waveSpeed;
        main.maxParticles = 200;
        main.startColor = Color.cyan;
        
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f;
        
        var velocityOverLifetime = particles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.radial = waveSpeed;
        
        // エフェクトを自動削除
        Destroy(waveEffect, main.startLifetime.constant + 1f);
    }
}
```

### AI重力制御システム

```csharp
public class AIGravityController : MonoBehaviour
{
    [Header("AI Settings")]
    public float reactionTime = 0.5f;
    public float predictionAccuracy = 0.8f;
    public bool enableLearning = true;
    
    [Header("Behavior")]
    public float gravityFlipCooldown = 1f;
    public float dangerDetectionRange = 10f;
    
    private float lastGravityFlip = 0f;
    private List<Vector3> obstacleMemory = new List<Vector3>();
    private Dictionary<string, float> behaviorWeights = new Dictionary<string, float>();
    
    private enum AIState
    {
        Normal,
        Avoiding,
        Pursuing,
        Learning
    }
    
    private AIState currentState = AIState.Normal;
    
    private void Start()
    {
        InitializeBehaviorWeights();
        StartCoroutine(AIThinkLoop());
    }
    
    private void InitializeBehaviorWeights()
    {
        behaviorWeights["avoid_spikes"] = 1.0f;
        behaviorWeights["collect_items"] = 0.7f;
        behaviorWeights["maintain_speed"] = 0.5f;
        behaviorWeights["predict_trajectory"] = 0.9f;
    }
    
    private IEnumerator AIThinkLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(reactionTime);
            
            AnalyzeEnvironment();
            MakeDecision();
            
            if (enableLearning)
            {
                UpdateLearning();
            }
        }
    }
    
    private void AnalyzeEnvironment()
    {
        // 周囲の障害物検出
        Collider2D[] obstacles = Physics2D.OverlapCircleAll(transform.position, dangerDetectionRange);
        
        Vector3 nearestDangerPosition = Vector3.zero;
        float nearestDangerDistance = float.MaxValue;
        
        foreach (var obstacle in obstacles)
        {
            if (obstacle.CompareTag("Obstacle"))
            {
                float distance = Vector3.Distance(transform.position, obstacle.transform.position);
                if (distance < nearestDangerDistance)
                {
                    nearestDangerDistance = distance;
                    nearestDangerPosition = obstacle.transform.position;
                }
            }
        }
        
        // 軌道予測
        if (nearestDangerDistance < dangerDetectionRange)
        {
            PredictCollision(nearestDangerPosition);
        }
    }
    
    private void PredictCollision(Vector3 dangerPosition)
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        Vector2 currentVelocity = rb.velocity;
        Vector2 currentGravity = GravitySystem.Instance.GetGravityAtPosition(transform.position);
        
        // 現在の軌道で衝突するか予測
        for (float t = 0.1f; t <= 3f; t += 0.1f)
        {
            Vector2 futurePosition = GravityPhysicsUtils.CalculateTrajectory(
                transform.position, currentVelocity, currentGravity, t);
            
            float distanceToDanger = Vector2.Distance(futurePosition, dangerPosition);
            
            if (distanceToDanger < 2f) // 衝突予測
            {
                float avoidanceChance = Random.Range(0f, 1f);
                if (avoidanceChance < predictionAccuracy)
                {
                    RequestGravityFlip("collision_avoidance");
                }
                break;
            }
        }
    }
    
    private void MakeDecision()
    {
        switch (currentState)
        {
            case AIState.Normal:
                HandleNormalBehavior();
                break;
            case AIState.Avoiding:
                HandleAvoidanceBehavior();
                break;
            case AIState.Pursuing:
                HandlePursuitBehavior();
                break;
        }
    }
    
    private void HandleNormalBehavior()
    {
        // 基本的な進行ルーティン
        // 収集アイテムの検出
        Collider2D[] collectibles = Physics2D.OverlapCircleAll(transform.position, dangerDetectionRange);
        
        foreach (var collectible in collectibles)
        {
            if (collectible.CompareTag("Collectible"))
            {
                Vector3 itemPosition = collectible.transform.position;
                if (ShouldFlipToReachItem(itemPosition))
                {
                    RequestGravityFlip("item_collection");
                }
                break;
            }
        }
    }
    
    private bool ShouldFlipToReachItem(Vector3 itemPosition)
    {
        // 重力反転で到達しやすくなるかの判定
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        Vector2 currentVelocity = rb.velocity;
        Vector2 currentGravity = GravitySystem.Instance.GetGravityAtPosition(transform.position);
        Vector2 flippedGravity = -currentGravity;
        
        // 現在の軌道での最接近距離
        float currentBestDistance = CalculateBestApproachDistance(itemPosition, currentVelocity, currentGravity);
        
        // 反転後の軌道での最接近距離
        float flippedBestDistance = CalculateBestApproachDistance(itemPosition, currentVelocity, flippedGravity);
        
        return flippedBestDistance < currentBestDistance - 1f; // 1unit以上改善される場合
    }
    
    private float CalculateBestApproachDistance(Vector3 target, Vector2 velocity, Vector2 gravity)
    {
        float bestDistance = float.MaxValue;
        
        for (float t = 0.1f; t <= 5f; t += 0.2f)
        {
            Vector2 position = GravityPhysicsUtils.CalculateTrajectory(transform.position, velocity, gravity, t);
            float distance = Vector2.Distance(position, target);
            
            if (distance < bestDistance)
            {
                bestDistance = distance;
            }
        }
        
        return bestDistance;
    }
    
    private void HandleAvoidanceBehavior()
    {
        // 回避行動中の処理
        currentState = AIState.Normal; // 一時的に通常状態に戻る
    }
    
    private void HandlePursuitBehavior()
    {
        // 追跡行動中の処理
        currentState = AIState.Normal; // 一時的に通常状態に戻す
    }
    
    private void RequestGravityFlip(string reason)
    {
        if (Time.time - lastGravityFlip < gravityFlipCooldown) return;
        
        Debug.Log($"AI requested gravity flip: {reason}");
        
        // プレイヤーコントローラーに重力反転を要求
        var playerController = GetComponent<GravityFlipLab.Player.PlayerController>();
        if (playerController != null)
        {
            playerController.FlipGravity();
            lastGravityFlip = Time.time;
            
            // 学習データに記録
            if (enableLearning)
            {
                RecordDecision(reason, true);
            }
        }
    }
    
    private void UpdateLearning()
    {
        // 簡単な学習システム
        // 最近の判断結果に基づいて行動重みを調整
        
        // 例: 衝突回避が成功した場合は重みを増加
        if (behaviorWeights.ContainsKey("avoid_spikes"))
        {
            // 実際の実装では、より詳細な結果評価が必要
            float currentWeight = behaviorWeights["avoid_spikes"];
            behaviorWeights["avoid_spikes"] = Mathf.Lerp(currentWeight, 1.2f, 0.1f);
        }
    }
    
    private void RecordDecision(string reason, bool wasExecuted)
    {
        // 決定履歴の記録（学習用）
        Debug.Log($"Decision recorded: {reason}, Executed: {wasExecuted}");
    }
    
    private void OnDrawGizmosSelected()
    {
        // AI の検出範囲を可視化
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireeSphere(transform.position, dangerDetectionRange);
        
        // 障害物メモリの可視化
        Gizmos.color = Color.red;
        foreach (var obstacle in obstacleMemory)
        {
            Gizmos.DrawWireCube(obstacle, Vector3.one * 0.5f);
        }
    }
}
```

---

## 結論

Gravity Flip Lab の重力反転・物理システムは、ゲームの核となる革新的なメカニクスを実現する包括的なシステムです。このドキュメントで詳述した設計により、以下の優れた特徴が実現されます：

### 技術的優位性

1. **精密な物理演算**: Unity Physics2Dとの深い統合による自然な物理挙動
2. **柔軟な重力制御**: グローバルと局所重力の組み合わせによる多様な表現
3. **滑らかな遷移**: アニメーションカーブを活用した美しい重力変更
4. **高度な予測システム**: 軌道計算による先を見据えたゲームプレイ
5. **最適化されたパフォーマンス**: 大規模シーンでも安定した動作

### ゲームプレイへの貢献

- **直感的な操作**: 0.1秒の瞬間反転による爽快感
- **戦略的深度**: 重力予測と軌道計算による戦略性
- **環境との相互作用**: 多様な重力ゾーンによる豊かな体験
- **学習可能な難易度**: AIシステムによる適応的な挑戦

### 開発・保守面の利点

- **モジュラー設計**: 独立したコンポーネント設計による保守性の向上
- **拡張性**: 新しい重力効果や物理挙動の追加が容易
- **デバッグ支援**: 包括的なデバッグツールと監視システム
- **パフォーマンス監視**: リアルタイムな性能追跡と最適化提案
- **エラー耐性**: 異常値検出と自動復旧機能

### 実装の特徴

このシステムは以下の革新的な技術を採用しています：

#### 1. ハイブリッド重力システム
- **グローバル重力**: ゲーム全体の基準となる重力
- **局所重力ゾーン**: 特定エリアでの特殊重力効果
- **動的重力場**: 時間変化する重力パターン
- **AI制御重力**: 人工知能による自動重力制御

#### 2. 高精度物理演算
- **軌道予測**: 数学的に正確な放物線計算
- **衝突予測**: 将来の衝突点の事前計算
- **慣性保持**: 物理法則に基づく自然な慣性
- **エネルギー保存**: 重力変更時のエネルギー保存

#### 3. パフォーマンス最適化技術
- **空間分割**: グリッドベースの効率的な重力計算
- **計算キャッシュ**: 頻繁な計算結果のキャッシュ化
- **フレーム分散**: 重い処理の複数フレーム分散
- **オブジェクトプーリング**: メモリ効率的なエフェクト管理

### 今後の発展可能性

このシステムは以下のような拡張が可能です：

#### 技術的拡張
- **3D重力システム**: 3次元空間での重力制御
- **相対論的効果**: 時間遅延や空間歪みの表現
- **量子重力**: 確率的重力場の実装
- **重力レンズ効果**: 光の屈曲による視覚効果

#### ゲームプレイ拡張
- **協力プレイ**: 複数プレイヤーでの重力制御
- **重力パズル**: 複雑な重力謎解き要素
- **重力武器**: 重力を使った戦闘システム
- **重力建築**: 重力を利用した建設要素

#### AI・機械学習統合
- **適応型AI**: プレイヤーの行動パターン学習
- **動的難易度調整**: AIによる難易度自動調整
- **予測システム**: 機械学習による軌道予測の高精度化
- **行動分析**: プレイヤー行動の詳細分析

### 実装時の重要なポイント

#### 1. 物理演算の精度
```csharp
// 高精度な重力計算のために必要な設定
Time.fixedDeltaTime = 0.01f; // 100fps物理更新
Physics2D.velocityIterations = 12; // 高精度速度計算
Physics2D.positionIterations = 8;  // 高精度位置計算
```

#### 2. メモリ管理
- 重力計算結果のキャッシュ管理
- 不要なオブジェクトの自動削除
- パーティクルエフェクトのプール管理

#### 3. ユーザビリティ
- 視覚的な重力方向表示
- 軌道予測線の表示
- 音響フィードバックの実装

### 品質保証

このシステムは以下の品質保証手法を採用しています：

#### 1. テスト戦略
- **ユニットテスト**: 個別コンポーネントのテスト
- **統合テスト**: システム間連携のテスト
- **パフォーマンステスト**: 負荷状態でのテスト
- **物理精度テスト**: 数学的正確性のテスト

#### 2. 監視システム
- **リアルタイム性能監視**: FPS、メモリ使用量の追跡
- **物理演算精度監視**: 計算誤差の検出
- **異常値検出**: 不正な物理状態の検出
- **ユーザー行動分析**: プレイパターンの分析

#### 3. エラー処理
- **グレースフルデグラデーション**: 段階的機能低下
- **自動復旧**: 異常状態からの自動回復
- **フォールバック**: 代替システムへの切り替え
- **ログ記録**: 詳細なエラー情報の記録

---

## 付録

### A. 数学的背景

#### 重力場の数学的表現

重力場 **g** は位置 **r** の関数として以下のように表現されます：

```
g(r) = -∇φ(r)
```

ここで φ(r) は重力ポテンシャルです。

#### 軌道方程式

重力場における物体の軌道は以下の運動方程式に従います：

```
d²r/dt² = g(r)
```

これを積分することで位置と速度の時間発展を計算できます。

#### エネルギー保存則

重力場における力学的エネルギーは以下のように保存されます：

```
E = (1/2)mv² + mφ(r) = const
```

### B. 実装チェックリスト

#### 基本実装
- [ ] GravitySystemシングルトンの実装
- [ ] LocalGravityZoneクラスの実装
- [ ] GravityAffectedObjectクラスの実装
- [ ] 重力反転メソッドの実装
- [ ] 物理エンジンとの統合

#### 特殊機能
- [ ] 重力井戸の実装
- [ ] 風のトンネルの実装
- [ ] スプリングプラットフォームの実装
- [ ] 斜面物理の実装
- [ ] 慣性システムの実装

#### 最適化
- [ ] 重力計算キャッシュの実装
- [ ] 空間分割システムの実装
- [ ] パフォーマンス監視の実装
- [ ] メモリ最適化の実装
- [ ] エフェクトプールの実装

#### デバッグ・テスト
- [ ] デバッグツールの実装
- [ ] 性能測定システムの実装
- [ ] 自動テストの実装
- [ ] エラーハンドリングの実装
- [ ] ログシステムの実装

### C. 参考資料

#### 物理学的参考文献
- "Classical Mechanics" by Herbert Goldstein
- "Foundations of Physics" by R. Bruce Lindsay
- "Game Physics Engine Development" by Ian Millington

#### Unity技術資料
- Unity Physics 2D Documentation
- Unity Performance Optimization Guide
- Unity Scripting Best Practices

#### 数学的参考資料
- "Calculus of Variations" by I.M. Gelfand
- "Vector Analysis" by Murray R. Spiegel
- "Numerical Methods" by Richard L. Burden

---

## 終わりに

Gravity Flip Lab の重力反転・物理システムは、革新的なゲームプレイと高度な技術実装を融合した、次世代のゲーム物理システムです。このドキュメントを参考に、開発チームは効率的で高品質な実装を実現し、プレイヤーに画期的なゲーム体験を提供できるでしょう。

システムの各コンポーネントは独立性を保ちながらも密接に連携し、拡張性と保守性を両立した設計となっています。また、パフォーマンス最適化と品質保証の仕組みにより、商業レベルの製品として十分な品質を確保しています。

このシステムが、Gravity Flip Lab の成功と、ゲーム業界における新しい物理表現の可能性を切り開く一助となることを期待しています。