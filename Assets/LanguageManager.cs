using UnityEngine;
using UnityEngine.Localization.Settings;
using BackEnd;
using BackEnd.GlobalSupport;

public class LanguageManager : MonoBehaviour
{
    // 한국으로 국가 코드 저장
    public void SetCountryKorea()
    {
        Backend.BMember.UpdateCountryCode(CountryCode.SouthKorea, callback =>
        {
            if (callback.IsSuccess())
            {
                Debug.Log("국가 코드가 한국(KR)으로 설정되었습니다.");
            }
            else
            {
                Debug.LogError("국가 코드 설정 실패");
            }
        });
    }

    // 영어로 국가 코드 저장
    public void SetCountryEnglish()
    {
        Backend.BMember.UpdateCountryCode(CountryCode.UnitedStates, callback =>
        {
            if (callback.IsSuccess())
            {
                Debug.Log("국가 코드가 미국(US)으로 설정되었습니다.");
            }
            else
            {
                Debug.LogError("국가 코드 설정 실패");
            }
        });
    }

    // 국가 코드를 확인하여 언어 변경
    public void UpdateLanguageByCountry()
    {
        Backend.BMember.GetMyCountryCode(callback =>
        {
            if (!callback.IsSuccess())
            {
                Debug.LogError("국가 코드를 불러오지 못했습니다.");
                return;
            }

            string country = callback.GetReturnValuetoJSON()["country"]["S"].ToString();

            if (country == "KR")
            {
                SetKorean();
            }
            else
            {
                SetEnglish();
            }
        });
    }

    // 한국어 적용
    public void SetKorean()
    {
        LocalizationSettings.SelectedLocale =
            LocalizationSettings.AvailableLocales.GetLocale("ko");
    }

    // 영어 적용
    public void SetEnglish()
    {
        LocalizationSettings.SelectedLocale =
            LocalizationSettings.AvailableLocales.GetLocale("en");
    }
    
}