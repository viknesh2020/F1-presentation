using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Random = UnityEngine.Random;

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
   public TMP_Text quoteText;
   public List<String> quotes = new List<String>();

   private int currentSlideIndex = 0;
   private Coroutine slideCoroutine;
   private float globalTimer = 0f;
   private bool countdownActive = false;
   private bool handleSlideOn = false;
   
   private float startTime;
   private float endTime;
   
   [HideInInspector]public bool isAnimEnd = false;

   private void Start()
   {
      startLights.SetActive(false);
      introPanel.SetActive(false);
      lights.SetActive(false);
      
      if(countdownPanel != null)
         countdownPanel.SetActive(false);
      
      // Initialize slides
      for (int i = 0; i < slides.Length; i++)
      {
         slides[i].alpha = (i == 0) ? 1 : 0;
         slides[i].gameObject.SetActive(i == 0);
      }
      
      StartCoroutine("SlidesRun");
   }
   
   void Update()
   {
      if(!handleSlideOn) return;
        
         //Debug.Log(globalTimer);

      if (durations[currentSlideIndex] > 10f)
         //Proceed only when the current duration of the slide is more than 10 seconds.
      
         if (!countdownActive && Time.time>endTime-10f)
         {
            //Debug.Log("End " +endTime);
            StartCoroutine(ShowCountdownCircular(10));
         }

         /*if (globalTimer >= 60f)
         {
            globalTimer = 0f;
         }*/

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

      yield return new WaitForSeconds(4f);
      
      introPanel.SetActive(true);
      LeanTween.moveX(introPanel, 0f, 3f).setEase(LeanTweenType.easeInOutQuart);
      slideCoroutine = StartCoroutine(HandleSlide(currentSlideIndex));
   }
   
   IEnumerator HandleSlide(int index)
   {
      handleSlideOn = true;
      //float timer = 0f;
      
       startTime = Time.time;
       //Debug.Log("Start Time: " +startTime);
       endTime = startTime + durations[index];
      
      //while (timer < durations[index])
      while(Time.time <= endTime)
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
         
         //Debug.Log("End Time: " +endTime);
         //timer += Time.deltaTime;
         yield return null;
      }

      //endTime = 0f;
      //Debug.Log("End Time after loop: " +endTime);
      
      // Auto advance
      MoveToSlide(currentSlideIndex + 1);
   }
   
   void MoveToSlide(int newIndex)
   {
      handleSlideOn = false;
      if (newIndex < 0 || newIndex >= slides.Length) return;
      slideCoroutine = StartCoroutine(SlideTransition(currentSlideIndex, newIndex));
      //slideStartTime = Time.time;
      currentSlideIndex = newIndex;
      //Debug.Log("Current Slide Duration: " +durations[currentSlideIndex]);
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
      countdownPanel.SetActive(true);
      countdownActive = true;

      // Move countdownPanel offscreen (to the right)
      RectTransform rect = countdownPanel.GetComponent<RectTransform>();
      Vector2 onScreenPos = new Vector2(-283f, 85f); // Target position (should be bottom-right)
      Vector2 offScreenPos = new Vector2(283f, 85f); // Slide in from 400px to the right

      // Immediately place it offscreen
      rect.anchoredPosition = offScreenPos;

      // Animate it into place
      LeanTween.move(rect, onScreenPos, 0.5f).setEaseOutExpo();
      quoteText.text = quotes[Random.Range(0, quotes.Count)];
      
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
