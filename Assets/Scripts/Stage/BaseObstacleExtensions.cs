using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityFlipLab.Stage
{
    /// <summary>
    /// 既存のBaseObstacleとPlayerControllerを統合するための拡張
    /// </summary>
    public static class BaseObstacleExtensions
    {
        /// <summary>
        /// 既存のPlayerControllerと連携したダメージ処理
        /// </summary>
        public static void DealDamageToExistingPlayer(this BaseObstacle obstacle, GameObject target)
        {
            if (!obstacle.IsTargetValid(target)) return;

            // 既存のPlayerStageAdapterを探す
            var stageAdapter = target.GetComponent<Player.PlayerStageAdapter>();
            if (stageAdapter != null)
            {
                stageAdapter.HandlePlayerDamage();
                obstacle.OnTargetDamaged?.Invoke(obstacle, target);
                return;
            }

            // フォールバック: 既存のPlayerControllerを直接呼び出し
            var playerController = target.GetComponent<Player.PlayerController>();
            if (playerController != null)
            {
                playerController.TakeDamage();
                obstacle.OnTargetDamaged?.Invoke(obstacle, target);
            }
        }
    }
}