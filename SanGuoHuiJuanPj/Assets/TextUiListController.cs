using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TextUiListController : MonoBehaviour
{
    [SerializeField] private TextUi _prefab;
    [SerializeField] private Transform content;
    private List<TextUi> _list;
    public IReadOnlyList<TextUi> List => _list;
    public void Init()
    {
        var items = content.GetComponentsInChildren<TextUi>();
        foreach (var item in items) item.Display(false);
        _list = new List<TextUi>();
    }
    public void AddUi(UnityAction<TextUi> initAction)
    {
        var ui = Instantiate(_prefab, content);
        _list.Add(ui);
        initAction(ui);
        ui.gameObject.SetActive(true);
    }
    public void ClearList()
    {
        foreach (var ui in _list.ToArray())
        {
            _list.Remove(ui);
            Destroy(ui.gameObject);
        }
    }
}