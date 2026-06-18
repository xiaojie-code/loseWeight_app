using UnityEngine;
using UnityEngine.UI;
using LoseWeight.Core;
using LoseWeight.Data;

namespace LoseWeight.UI.Pages
{
    public class CharacterSelectPage : MonoBehaviour
    {
        private Gender _selectedGender = Gender.Male;

        public void SelectMale() => SelectGender(Gender.Male);
        public void SelectFemale() => SelectGender(Gender.Female);

        public void SelectGender(Gender gender)
        {
            _selectedGender = gender;
            SetAlpha("MaleCard", gender == Gender.Male ? 0.6f : 0.2f);
            SetAlpha("FemaleCard", gender == Gender.Female ? 0.6f : 0.2f);
        }

        public void Confirm()
        {
            PlayerPrefs.SetInt("gender_selected", (int)_selectedGender);
            PlayerPrefs.Save();
            GameManager.Instance.ChangeState(GameState.MainMenu);
        }

        private void SetAlpha(string nodeName, float alpha)
        {
            var image = FindChild(transform, nodeName)?.GetComponent<Image>();
            if (image == null) return;
            var color = image.color;
            color.a = alpha;
            image.color = color;
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindChild(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
