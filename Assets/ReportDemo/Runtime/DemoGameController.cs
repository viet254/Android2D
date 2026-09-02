using System;
using UnityEngine;
using UnityEngine.UI;

namespace Android2D.ReportDemo
{
    /// <summary>
    /// Updates and draws the report demo's game objects: player, enemy and crystal.
    /// </summary>
    public sealed class DemoGameController : MonoBehaviour
    {
        private const float GroundY = 108f;

        private RectTransform playArea;
        private RectTransform player;
        private RectTransform enemy;
        private RectTransform crystal;
        private RectTransform leftControl;
        private RectTransform rightControl;
        private RectTransform jumpControl;
        private Text scoreText;
        private Text livesText;
        private Text messageText;
        private Action gameOver;

        private float playerX;
        private float playerHeight;
        private float verticalVelocity;
        private float enemyX;
        private float enemyDirection = -1f;
        private float crystalX;
        private float crystalPhase;
        private int score;
        private int lives = 3;
        private float invulnerableTime;
        private bool running;

        public void Initialize(RectTransform area, RectTransform playerRect, RectTransform enemyRect,
            RectTransform crystalRect, RectTransform left, RectTransform right, RectTransform jump,
            Text scoreLabel, Text livesLabel, Text messageLabel, Action onGameOver)
        {
            playArea = area;
            player = playerRect;
            enemy = enemyRect;
            crystal = crystalRect;
            leftControl = left;
            rightControl = right;
            jumpControl = jump;
            scoreText = scoreLabel;
            livesText = livesLabel;
            messageText = messageLabel;
            gameOver = onGameOver;
            ResetGame();
        }

        public void ResetGame()
        {
            playerX = -220f;
            playerHeight = 0f;
            verticalVelocity = 0f;
            enemyX = 320f;
            enemyDirection = -1f;
            crystalX = 80f;
            score = 0;
            lives = 3;
            invulnerableTime = 0f;
            running = true;
            UpdateHud();
            if (messageText != null)
            {
                messageText.text = "THU THẬP TINH THỂ • TRÁNH BÓNG ĐÊM";
            }
        }

        public void SetRunning(bool value)
        {
            running = value;
        }

        private void Update()
        {
            if (!running || playArea == null)
            {
                return;
            }

            float deltaTime = Time.unscaledDeltaTime;
            float width = Mathf.Max(800f, playArea.rect.width);
            float halfWidth = width * 0.5f;

            float horizontal = ReportInput.KeyboardHorizontal();
            if (ReportInput.IsHeld(leftControl)) horizontal -= 1f;
            if (ReportInput.IsHeld(rightControl)) horizontal += 1f;
            horizontal = Mathf.Clamp(horizontal, -1f, 1f);
            playerX = Mathf.Clamp(playerX + horizontal * 360f * deltaTime, -halfWidth + 70f, halfWidth - 70f);

            bool grounded = playerHeight <= 0.01f;
            if (grounded && (ReportInput.JumpPressed() || ReportInput.WasPressed(jumpControl)))
            {
                verticalVelocity = 720f;
            }

            verticalVelocity -= 1500f * deltaTime;
            playerHeight = Mathf.Max(0f, playerHeight + verticalVelocity * deltaTime);
            if (playerHeight <= 0f)
            {
                verticalVelocity = 0f;
            }

            enemyX += enemyDirection * 190f * deltaTime;
            if (enemyX < -halfWidth + 80f || enemyX > halfWidth - 80f)
            {
                enemyX = Mathf.Clamp(enemyX, -halfWidth + 80f, halfWidth - 80f);
                enemyDirection *= -1f;
            }

            crystalPhase += deltaTime * 3f;
            invulnerableTime = Mathf.Max(0f, invulnerableTime - deltaTime);
            ApplyTransforms(horizontal);
            CheckCollisions(halfWidth);
        }

        private void ApplyTransforms(float horizontal)
        {
            player.anchoredPosition = new Vector2(playerX, GroundY + playerHeight);
            if (Mathf.Abs(horizontal) > 0.05f)
            {
                Vector3 scale = player.localScale;
                scale.x = Mathf.Abs(scale.x) * Mathf.Sign(horizontal);
                player.localScale = scale;
            }

            enemy.anchoredPosition = new Vector2(enemyX, GroundY + 6f + Mathf.Sin(Time.unscaledTime * 4f) * 5f);
            enemy.localScale = new Vector3(Mathf.Sign(enemyDirection), 1f, 1f);
            crystal.anchoredPosition = new Vector2(crystalX, GroundY + 105f + Mathf.Sin(crystalPhase) * 18f);
            crystal.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(crystalPhase * 0.7f) * 12f);

            Image playerImage = player.GetComponent<Image>();
            playerImage.color = invulnerableTime > 0f && Mathf.FloorToInt(invulnerableTime * 12f) % 2 == 0
                ? new Color(1f, 1f, 1f, 0.35f)
                : Color.white;
        }

        private void CheckCollisions(float halfWidth)
        {
            Vector2 playerPosition = player.anchoredPosition;
            Vector2 crystalPosition = crystal.anchoredPosition;
            if (Mathf.Abs(playerPosition.x - crystalPosition.x) < 65f &&
                Mathf.Abs(playerPosition.y - crystalPosition.y) < 90f)
            {
                score += 10;
                crystalX = UnityEngine.Random.Range(-halfWidth + 110f, halfWidth - 110f);
                crystalPhase = 0f;
                messageText.text = "+10 NĂNG LƯỢNG!";
                Invoke(nameof(RestoreMessage), 0.8f);
                UpdateHud();
            }

            Vector2 enemyPosition = enemy.anchoredPosition;
            if (invulnerableTime <= 0f && Mathf.Abs(playerPosition.x - enemyPosition.x) < 72f &&
                Mathf.Abs(playerPosition.y - enemyPosition.y) < 90f)
            {
                lives--;
                invulnerableTime = 1.5f;
                playerX -= Mathf.Sign(enemyX - playerX) * 120f;
                UpdateHud();
                if (lives <= 0)
                {
                    running = false;
                    messageText.text = "HẾT LƯỢT • CHẠM CHƠI LẠI";
                    gameOver?.Invoke();
                }
                else
                {
                    messageText.text = "CẨN THẬN BÓNG ĐÊM!";
                    Invoke(nameof(RestoreMessage), 1f);
                }
            }
        }

        private void RestoreMessage()
        {
            if (running && messageText != null)
            {
                messageText.text = "THU THẬP TINH THỂ • TRÁNH BÓNG ĐÊM";
            }
        }

        private void UpdateHud()
        {
            if (scoreText != null) scoreText.text = $"NĂNG LƯỢNG  {score:000}";
            if (livesText != null) livesText.text = $"MẠNG  {new string('♥', Mathf.Max(0, lives))}";
        }
    }
}
