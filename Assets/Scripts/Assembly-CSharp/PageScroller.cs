using System;
using UnityEngine;

public class PageScroller : MonoBehaviour
{
    public delegate void PageChanged(int oldPage, int newPage);
    [SerializeField]
    private Transform m_scrollPivot;
    private Vector3 m_lastInputPos;
    private Vector3 m_pointerDownPos;
    private Camera m_hudCamera;
    private int m_pageCount = 1;
    public int PageCount
    {
        get => m_pageCount;
        set
        {
            if (value > 0)
            {
                m_pageCount = value;
                if (CurrentPage >= m_pageCount)
                {
                    ScrollToPage(m_pageCount - 1);
                }

                return;
            }

            throw new ArgumentException("PageCount can't be less then 1");
        }
    }

    public int CurrentPage { get; private set; }

    public event PageChanged OnPageChanged;
    private void Start()
    {
        m_hudCamera = Singleton<GuiManager>.Instance.FindCamera();
    }

    private void OnEnable()
    {
        m_hudCamera = Singleton<GuiManager>.Instance.FindCamera();
    }

    public void ScrollToPage(int newPage)
    {
        if (newPage >= 0 && newPage < m_pageCount && newPage != CurrentPage)
        {
            this.OnPageChanged?.Invoke(CurrentPage, newPage);
            CurrentPage = newPage;
        }
    }

    public void SetPage(int newPage)
    {
        if (newPage >= 0 && newPage < m_pageCount && newPage != CurrentPage)
        {
            this.OnPageChanged?.Invoke(CurrentPage, newPage);
            CurrentPage = newPage;
            m_scrollPivot.localPosition = GetTargetPosition(CurrentPage);
        }
    }

    private Vector3 GetTargetPosition(int pageNum)
    {
        if (!m_hudCamera)
        {
            m_hudCamera = Singleton<GuiManager>.Instance.FindCamera();
        }

        return new Vector3(0f - m_hudCamera.ScreenToWorldPoint(new Vector3(Screen.width / 2 + Screen.width * pageNum, 0f, 0f)).x + m_hudCamera.transform.position.x, m_scrollPivot.localPosition.y, m_scrollPivot.localPosition.z);
    }

    private void Update()
    {
        GuiManager.Pointer pointer = GuiManager.GetPointer();
        if (pointer.down)
        {
            m_pointerDownPos = (m_lastInputPos = pointer.position);
        }

        if (pointer.dragging && !LootCrateOpenDialog.DialogOpen && !TextDialog.DialogOpen)
        {
            Vector3 vector = m_hudCamera.ScreenToWorldPoint(m_lastInputPos);
            Vector3 vector2 = m_hudCamera.ScreenToWorldPoint(pointer.position);
            m_lastInputPos = pointer.position;
            float num = vector2.x - vector.x;
            m_scrollPivot.localPosition = new Vector3(m_scrollPivot.localPosition.x + num, m_scrollPivot.localPosition.y, m_scrollPivot.localPosition.z);
            Vector3 vector3 = m_hudCamera.ScreenToWorldPoint(m_pointerDownPos);
            if (Mathf.Abs(vector2.x - vector3.x) > 1f)
            {
                Singleton<GuiManager>.Instance.ResetFocus();
            }
        }

        if (pointer.up)
        {
            float num2 = m_lastInputPos.x - m_pointerDownPos.x;
            if (Mathf.Abs(num2) > (float)(Screen.width / 16))
            {
                int page = CurrentPage;
                CurrentPage += ((!(num2 >= 0f)) ? 1 : (-1));
                CurrentPage = Mathf.Clamp(CurrentPage, 0, m_pageCount - 1);
                if (page != CurrentPage)
                {
                    this.OnPageChanged(page, CurrentPage);
                }
            }
        }

        if (!pointer.down && !pointer.dragging)
        {
            Vector3 targetPosition = GetTargetPosition(CurrentPage);
            if (Vector3.SqrMagnitude(targetPosition - m_scrollPivot.localPosition) > 1E-05f)
            {
                m_scrollPivot.localPosition += (targetPosition - m_scrollPivot.localPosition) * Time.unscaledDeltaTime * 4f;
            }
        }
    }
}