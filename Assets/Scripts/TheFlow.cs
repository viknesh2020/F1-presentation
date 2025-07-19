using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TheFlow : MonoBehaviour
{
   [Header("Initial UX")]
   public GameObject startLights;
   public CinemachineCamera focusCamera;
   public GameObject introPanel;
   public GameObject lights;
   
   [Header("Slides")]
   public CanvasGroup[] slides;            // All slide panels with CanvasGroup
   public float[] durations;               // Per-slide visibility duration in seconds
   public float fadeDuration = 1f;

   [Header("Countdown UI")]
   public GameObject countdownPanel;       // Parent GameObject for countdown (inactive by default)
   public Image fillImage;                 // Image with radial fill
   public TextMeshProUGUI countdownText;   // Centered countdown number

   private int currentSlideIndex = 0;
   private Coroutine slideCoroutine;
   private float globalTimer = 0f;
   private bool countdownActive = false;
   
   [HideInInspector]public bool isAnimEnd = false;

   private void Start()
   {
      startLights.SetActive(false);
      introPanel.SetActive(false);
      lights.SetActive(false);
      
      // Initialize slides
      for (int i = 0; i < slides.Length; i++)
      {
         slides[i].alpha = (i == 0) ? 1 : 0;
         slides[i].gameObject.SetActive(i == 0);
      }

      if (countdownPanel != null)
         countdownPanel.SetActive(false);
      
      StartCoroutine("SlidesRun");
   }
   
   void Update()
   {
      globalTimer += Time.deltaTime;

      // Trigger countdown every 50 seconds globally
      if (!countdownActive && globalTimer >= 50f)
      {
         StartCoroutine(ShowCountdownCircular(10));
         globalTimer = 0f;
      }

      // Navigation input
      if (Input.GetKeyDown(KeyCode.DownArrow))
      {
         if (slideCoroutine != null) StopCoroutine(slideCoroutine);
         MoveToSlide(currentSlideIndex + 1);
      }
      else if (Input.GetKeyDown(KeyCode.UpArrow))
      {
         if (slideCoroutine != null) StopCoroutine(slideCoroutine);
         MoveToSlide(currentSlideIndex - 1);
      }
   }

   IEnumerator SlidesRun()
   {
      yield return new WaitUntil(()=> Input.GetKeyDown(KeyCode.Space));
      startLights.SetActive(true);
      yield return new WaitUntil(() => isAnimEnd);
      yield return new WaitForSeconds(1f);
      startLights.SetActive(false);
      
      lights.SetActive(true);
      focusCamera.Priority = 2;

      yield return new WaitForSeconds(3f);
      
      introPanel.SetActive(true);
      LeanTween.moveX(introPanel, 0f, 3f).setEase(LeanTweenType.easeInOutQuart);
      yield return new WaitForSeconds(50f);
      slideCoroutine = StartCoroutine(HandleSlide(currentSlideIndex));
   }
   
   IEnumerator HandleSlide(int index)
   {
      float timer = 0f;
      while (timer < durations[index])
      {
         // Slide skip input
         if (Input.GetKeyDown(KeyCode.DownArrow))
         {
            MoveToSlide(currentSlideIndex + 1);
            yield break;
         }
         else if (Input.GetKeyDown(KeyCode.UpArrow))
         {
            MoveToSlide(currentSlideIndex - 1);
            yield break;
         }

         timer += Time.deltaTime;
         yield return null;
      }

      // Auto advance
      MoveToSlide(currentSlideIndex + 1);
   }
   
   void MoveToSlide(int newIndex)
   {
      if (newIndex < 0 || newIndex >= slides.Length) return;
      slideCoroutine = StartCoroutine(SlideTransition(currentSlideIndex, newIndex));
      currentSlideIndex = newIndex;
   }
   
   IEnumerator SlideTransition(int from, int to)
   {
      CanvasGroup fromSlide = slides[from];
      CanvasGroup toSlide = slides[to];

      toSlide.gameObject.SetActive(true);

      bool fadeOutComplete = false;
      bool fadeInComplete = false;

      LeanTween.alphaCanvas(fromSlide, 0f, fadeDuration).setOnComplete(() =>
      {
         fromSlide.gameObject.SetActive(false);
         fadeOutComplete = true;
      });

      LeanTween.alphaCanvas(toSlide, 1f, fadeDuration).setOnComplete(() =>
      {
         fadeInComplete = true;
      });

      // Wait for both fades to finish
      while (!fadeOutComplete || !fadeInComplete)
      {
         yield return null;
      }

      slideCoroutine = StartCoroutine(HandleSlide(to));
   }
   
   IEnumerator ShowCountdownCircular(int seconds)
   {
      countdownActive = true;

      // Move countdownPanel offscreen (to the right)
      RectTransform rect = countdownPanel.GetComponent<RectTransform>();
      Vector2 onScreenPos = rect.anchoredPosition; // Target position (should be bottom-right)
      Vector2 offScreenPos = onScreenPos + new Vector2(400f, 0f); // Slide in from 400px to the right

      // Immediately place it offscreen
      rect.anchoredPosition = offScreenPos;

      // Animate it into place
      LeanTween.move(rect, onScreenPos, 0.5f).setEaseOutExpo();

      float duration = seconds;
      float elapsed = 0f;

      while (elapsed < duration)
      {
         float t = elapsed / duration;
         int remaining = Mathf.CeilToInt(duration - elapsed);
         countdownText.text = remaining.ToString();

         fillImage.fillAmount = Mathf.Clamp01(1f - t);
         fillImage.color = Color.Lerp(Color.green, Color.red, t);

         elapsed += Time.deltaTime;
         yield return null;
      }

      countdownText.text = "0";
      fillImage.fillAmount = 0f;
      fillImage.color = Color.red;

      yield return new WaitForSeconds(0.5f);

      // Slide it out again smoothly (optional)
      LeanTween.move(rect, offScreenPos, 0.5f).setEaseInExpo();

      countdownActive = false;
   }

   public void SetAnimEnd()
   {
      isAnimEnd = true;
   }
}
