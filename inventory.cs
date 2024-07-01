using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Inventory : MonoBehaviour
{
	[SerializeField] private ItemList GlobalItemList;
	[SerializeField] private Animator PlayerAnimator;
	[SerializeField] private int SlotEquipped;
	[SerializeField] private RectTransform point;
	[SerializeField] private Vector2 DefaultPointPosition;
	[SerializeField] private float PointIncrement;
	[SerializeField] private Transform ToolParent;
	[SerializeField] private Image Frame;
	[SerializeField] private Transform MouseItem;
	[SerializeField] private TextMeshProUGUI MouseItemText;
	[SerializeField] private TextMeshProUGUI MouseItemBorder;
	[SerializeField] private Transform GhostObject;
	[SerializeField] private RoomManager RoomManager;

	private PlayerInputActions InputActions;
	private int OldSlotEquipped = 0;
	private bool InventoryOpen = false;
	private bool IsResult = false;
	private Frame HoldingObjectFrame;
	private Animator anim;
	private List<Image> FrameList = new List<Image>();
	private Image[] HotbarFrames = new Image[5];
	private List<Object> Objects = new List<Object>();
	private Image SelectedFrame;
	private int SelectedFrameIndex;
	private string FrameSelectedItem = "air";
	private int SelectedItemAmount;
	private List<string> ItemsQueued = new List<string>();
	private List<int> ItemQueuedAmts = new List<int>();

	private float DamageAdd;

	public static Inventory Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
	{
		InputActions = new PlayerInputActions();
		anim = GetComponent<Animator>();
		InputActions.Player.Enable();
		InputActions.Player.Slot1.performed += Slot1;
		InputActions.Player.Slot2.performed += Slot2;
		InputActions.Player.Slot3.performed += Slot3;
		InputActions.Player.Slot4.performed += Slot4;
		InputActions.Player.Slot5.performed += Slot5;
        InputActions.Player.Swing.performed += Swing;
        InputActions.Player.Inventory.performed += UpdateInventory;
		InstanstiateFrames();
    }

	private void InstanstiateFrames()
	{
		for (int y = 0; y < 5; y++)
		{
			for (int x = 0; x < 5; x++)
			{
				Image f = Instantiate(Frame, transform);
				f.rectTransform.anchoredPosition = new Vector2(233 + x * -75, f.rectTransform.anchoredPosition.y);
				if (y > 0)
				{
					f.rectTransform.anchoredPosition = new Vector2(f.rectTransform.anchoredPosition.x, 160 - 75 * y);
				}
				else
				{
					f.rectTransform.anchoredPosition = new Vector2(f.rectTransform.anchoredPosition.x, 180);
				}
				if (y == 0)
				{
					f.name = "hotbar frame";
                    HotbarFrames[4 - x] = f;
                }
				else
				{
                    f.name = "inventory frame";
                }
			}
		}
		
    }

	public void InstantiateObject(string ObjectName, Vector3 Position, int index, bool HasFrame = false)
	{
		Transform Object = Instantiate(Resources.Load<Transform>($"Object Prefabs/{ObjectName}"), Position, Quaternion.identity, RoomManager.GetRoom(false, index));
		Objects.Add(Object.GetComponent<Object>());
	}

	private void UpdateInventory(InputAction.CallbackContext context)
	{
		if (InventoryOpen)
		{
			anim.SetBool("Open", false);
			InventoryOpen = false;
			point.anchoredPosition += 1000 * Vector2.down;
		}
		else
		{
			anim.SetBool("Open", true);
			InventoryOpen = true;
		}
	}

	private void Slot1(InputAction.CallbackContext context)
	{
		OldSlotEquipped = SlotEquipped;
		SlotEquipped = 0;
		TryReplaceTool();
    }

	private void Slot2(InputAction.CallbackContext context)
	{
		OldSlotEquipped = SlotEquipped;
		SlotEquipped = 1;
		TryReplaceTool();
	}

	private void Slot3(InputAction.CallbackContext context)
	{
		OldSlotEquipped = SlotEquipped;
		SlotEquipped = 2;
		TryReplaceTool();
	}

	private void Slot4(InputAction.CallbackContext context)
	{
		OldSlotEquipped = SlotEquipped;
		SlotEquipped = 3;
		TryReplaceTool();
	}

	private void Slot5(InputAction.CallbackContext context)
	{
		OldSlotEquipped = SlotEquipped;
		SlotEquipped = 4;
		TryReplaceTool();
	}

	private void Swing(InputAction.CallbackContext context)
	{
		if (HoldingObjectFrame != null)
		{
            InstantiateObject(HoldingObjectFrame.GetItem(), GhostObject.transform.position, 0, true);
			HoldingObjectFrame.SetItemAmount(HoldingObjectFrame.GetItemAmount() - 1);
			if (HoldingObjectFrame.GetItemAmount() - 1 <= 0)
			{
                HoldingObjectFrame.GetComponent<Frame>().SetItem("air");
                HoldingObjectFrame.GetComponent<Frame>().SetItemAmount(0);
                ForceReplaceTool();
            }
        }
	}

	private void Update()
	{
		for (int i = 0; i < Objects.Count(); i++)
		{
			Objects[i].SetSelectedFrameIndex(SelectedFrameIndex);
			Objects[i].SetItemName(HotbarFrames[SlotEquipped].GetComponent<Frame>().GetItem());
			if (Objects[i].GetDurability() <= 0)
			{
				if (Objects[i].name.Contains("scrap wood"))
				{
					QueueItemAdd("wood scrap", 4);
                }
				if (Objects[i].name.Contains("rock"))
				{
					QueueItemAdd("stone", 1);
                }
				if (Objects[i].name.Contains("glowstar bush"))
				{
                    QueueItemAdd("glowstar", 3);
                    QueueItemAdd("stick", 2);
                }
                if (Objects[i].name.Contains("raw iron"))
                {
                    QueueItemAdd("raw iron shard", 2);
                }
                if (Objects[i].name.Contains("furnace"))
                {
                    QueueItemAdd("furnace", 1);
                }
                if (Objects[i].name.Contains("crafting bench"))
                {
                    QueueItemAdd("crafting bench", 1);
                }
                if (Objects[i].name.Contains("chest"))
                {
                    QueueItemAdd("chest", 1);
                }
                if (Objects[i].name.Contains("planter"))
                {
                    QueueItemAdd("planter", 1);
                }
                Destroy(Objects[i].gameObject);
				Objects.Remove(Objects[i]);
			}
		}
		Image MouseItemImage = MouseItem.GetComponentsInChildren<Image>()[0];
		MouseItem.transform.position = Vector2.LerpUnclamped(MouseItem.transform.position, Input.mousePosition, 35 * Time.deltaTime);
		if (!HotbarFrames[SlotEquipped].GetComponent<Frame>().GetItem().Contains("blueprint"))
		{
			if (HotbarFrames[SlotEquipped].GetComponent<Frame>().GetItem().Contains("axe"))
			{
				PlayerAnimator.SetInteger("tool", 2);
			}
			else if (HotbarFrames[SlotEquipped].GetComponent<Frame>().GetItem().Contains("pick"))
			{
				PlayerAnimator.SetInteger("tool", 3);
			}
			else if (HotbarFrames[SlotEquipped].GetComponent<Frame>().GetItem().Contains("sword") | HotbarFrames[SlotEquipped].GetComponent<Frame>().GetItem().Contains("stick") | HotbarFrames[SlotEquipped].GetComponent<Frame>().GetItem().Contains("stone"))
			{
				PlayerAnimator.SetInteger("tool", 4);
			}
			else if (HotbarFrames[SlotEquipped].GetComponent<Frame>().GetItemType() == "food")
			{
                PlayerAnimator.SetInteger("tool", 5);
            }
            else if (HotbarFrames[SlotEquipped].GetComponent<Frame>().GetItem().Contains("bow"))
            {
                PlayerAnimator.SetInteger("tool", 6);
            }

        }
		else
		{
			PlayerAnimator.SetInteger("tool", 0);
		}
		for (int i = 0; i < FrameList.Count; i++)
		{
			if (FrameList[i] != null)
			{
                FrameList[i].GetComponent<Frame>().SetDamageAdd(0);
                FrameList[i].GetComponent<Frame>().SetInventoryOpen(InventoryOpen);
				if (ItemsQueued.Count != 0 && !FrameList[i].name.Contains("Object"))
				{
					for (int j = 0; j < ItemsQueued.Count; j++) 
					{
						if (FrameList[i].GetComponent<Frame>().GetItem() == "air" | FrameList[i].GetComponent<Frame>().GetItem() == ItemsQueued[j])
						{
							FrameList[i].GetComponent<Frame>().SetItemAmount(ItemQueuedAmts[j] + FrameList[i].GetComponent<Frame>().GetItemAmount());
							FrameList[i].GetComponent<Frame>().SetItem(ItemsQueued[j]);
							if (FrameList[i].GetComponent<Frame>().GetItem() == ItemsQueued[j])
							{
								ItemsQueued.RemoveAt(j);
								ItemQueuedAmts.RemoveAt(j);
							}
						}
					}
				}
				if (InventoryOpen)
				{
					if (FrameList[i].name.Contains("hotbar"))
					{
						FrameList[i].GetComponent<Frame>().SetSelectedOffset(-11);
					}
					else if (FrameList[i].name.Contains("inventory"))
					{
						FrameList[i].GetComponent<Frame>().SetSelectedOffset(5);
					}
				}
				else
				{
					if (FrameList[i].name.Contains("hotbar"))
					{
						FrameList[i].GetComponent<Frame>().SetSelectedOffset(0);
					}
					else if (FrameList[i].name.Contains("inventory"))
					{
						FrameList[i].GetComponent<Frame>().SetSelectedOffset(-40);
						FrameList[i].GetComponent<Frame>().SetSelected(true);
					}
				}
				if (FrameList[i] == HotbarFrames[SlotEquipped])
				{
                    FrameList[i].GetComponent<Frame>().SetDamageAdd(DamageAdd);
                    FrameList[i].GetComponent<Frame>().SetSelected(true);
					if (FrameList[i].GetComponent<Frame>().GetItemType() == "object")
					{
                        GhostObject.gameObject.SetActive(true);
						GhostObject.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>($"Object Images/{FrameList[i].GetComponent<Frame>().GetItem()}");
						HoldingObjectFrame = FrameList[i].GetComponent<Frame>();
                    }
					else
					{
						GhostObject.gameObject.SetActive(false);
						HoldingObjectFrame = null;

                    }
					if (FrameList[i].GetComponent<Frame>().TouchingMouse())
					{
						if (Input.GetMouseButtonDown(1))
						{
							if (FrameSelectedItem == "air")
							{
								if (FrameList[i].GetComponent<Frame>().GetItem() != "air")
								{
									FrameSelectedItem = FrameList[i].GetComponent<Frame>().GetItem();
									SetMouseItem(FrameList[i]);
                                    TryReplaceToolSwap(FrameList[i].GetComponent<Frame>().GetItem());
                                    SelectedFrame = FrameList[i];
									SelectedFrameIndex = i;
								}
							}
							else
							{
								if (FrameSelectedItem == FrameList[i].GetComponent<Frame>().GetItem())
								{
									FrameList[i].GetComponent<Frame>().SetSelected(false);
									FrameList[i].GetComponent<Frame>().SetItem(FrameSelectedItem);
									FrameList[i].GetComponent<Frame>().SetItemAmount(SelectedItemAmount + FrameList[i].GetComponent<Frame>().GetItemAmount());
									FrameSelectedItem = "air";
									MouseItemImage.rectTransform.sizeDelta = Vector2.zero;
									SetMouseItemText("");
								}
								else if (!IsResult | FrameList[i].GetComponent<Frame>().GetItem() == "air")
								{
									if (!IsResult)
									{
										SelectedFrame.GetComponent<Frame>().SetItem(FrameList[i].GetComponent<Frame>().GetItem());
										SelectedFrame.GetComponent<Frame>().SetItemAmount(FrameList[i].GetComponent<Frame>().GetItemAmount());
									}
									SelectedFrame.GetComponent<Frame>().SetItem(FrameList[i].GetComponent<Frame>().GetItem());
									SelectedFrame.GetComponent<Frame>().SetItemAmount(FrameList[i].GetComponent<Frame>().GetItemAmount());
                                    TryReplaceToolSwap(FrameSelectedItem);
									FrameList[i].GetComponent<Frame>().SetSelected(false);
									FrameList[i].GetComponent<Frame>().SetItem(FrameSelectedItem);
									FrameList[i].GetComponent<Frame>().SetItemAmount(SelectedItemAmount);
									FrameSelectedItem = "air";
									MouseItemImage.rectTransform.sizeDelta = Vector2.zero;
									SetMouseItemText("");
								}
							}
						}
					}
				}
				else if (FrameList[i].GetComponent<Frame>().TouchingMouse())
				{
                    if (InventoryOpen)
					{
						FrameList[i].GetComponent<Frame>().SetSelected(true);
					}
					if (Input.GetMouseButtonDown(1))
					{
						if (FrameSelectedItem == "air")
						{
							if (FrameList[i].GetComponent<Frame>().GetItem() != "air")
							{
								FrameSelectedItem = FrameList[i].GetComponent<Frame>().GetItem();
								SetMouseItem(FrameList[i]);
								SelectedFrame = FrameList[i];
								SelectedFrameIndex = i;
								if (FrameList[i].name.Contains("Result"))
								{
									IsResult = true;
								}
								else
								{
									IsResult = false;
								}
							}
						}
						else if (FrameList[i].GetComponent<Frame>().IsPicky())
						{
                            //print(FrameList[i].GetComponent<Frame>().GetAcceptableItems()[0]);
                            if (FrameSelectedItem != "air" && (FrameList[i].GetComponent<Frame>().GetAcceptableItems().Contains(FrameSelectedItem) | FrameList[i].GetComponent<Frame>().GetAcceptableItems().Contains(GetFrameComponent(0).GetItemTypeByName(FrameSelectedItem))))
							{
								if (FrameSelectedItem == FrameList[i].GetComponent<Frame>().GetItem())
								{
									FrameList[i].GetComponent<Frame>().SetSelected(false);
									FrameList[i].GetComponent<Frame>().SetItem(FrameSelectedItem);
									FrameList[i].GetComponent<Frame>().SetItemAmount(SelectedItemAmount + FrameList[i].GetComponent<Frame>().GetItemAmount());
								}
								else
								{
									SelectedFrame.GetComponent<Frame>().SetItem(FrameList[i].GetComponent<Frame>().GetItem());
									SelectedFrame.GetComponent<Frame>().SetItemAmount(FrameList[i].GetComponent<Frame>().GetItemAmount());
									if (FrameList[SelectedFrameIndex] == HotbarFrames[SlotEquipped] && FrameList[i].GetComponent<Frame>().GetItem() != "air")
									{
										TryReplaceToolSwap(FrameList[i].GetComponent<Frame>().GetItem());
									}
									FrameList[i].GetComponent<Frame>().SetSelected(false);
									FrameList[i].GetComponent<Frame>().SetItem(FrameSelectedItem);
									FrameList[i].GetComponent<Frame>().SetItemAmount(SelectedItemAmount);
								}
								FrameSelectedItem = "air";
								MouseItemImage.rectTransform.sizeDelta = Vector2.zero;
								SetMouseItemText("");
							}
						}
						else
						{
							if (FrameSelectedItem != "air")
							{
								if (FrameSelectedItem == FrameList[i].GetComponent<Frame>().GetItem())
								{
									FrameList[i].GetComponent<Frame>().SetSelected(false);
									FrameList[i].GetComponent<Frame>().SetItem(FrameSelectedItem);
									FrameList[i].GetComponent<Frame>().SetItemAmount(SelectedItemAmount + FrameList[i].GetComponent<Frame>().GetItemAmount());
									FrameSelectedItem = "air";
									MouseItemImage.rectTransform.sizeDelta = Vector2.zero;
									SetMouseItemText("");
								}
								else
								{
									if (!IsResult | FrameList[i].GetComponent<Frame>().GetItem() == "air")
									{
										if (!IsResult)
										{
											SelectedFrame.GetComponent<Frame>().SetItem(FrameList[i].GetComponent<Frame>().GetItem());
											SelectedFrame.GetComponent<Frame>().SetItemAmount(FrameList[i].GetComponent<Frame>().GetItemAmount());
										}
											
										if (FrameList[SelectedFrameIndex] == HotbarFrames[SlotEquipped] && FrameList[i].GetComponent<Frame>().GetItem() != "air")
										{
											TryReplaceToolSwap(FrameList[i].GetComponent<Frame>().GetItem());
										}
										FrameList[i].GetComponent<Frame>().SetSelected(false);
										FrameList[i].GetComponent<Frame>().SetItem(FrameSelectedItem);
										FrameList[i].GetComponent<Frame>().SetItemAmount(SelectedItemAmount);
										FrameSelectedItem = "air";
										MouseItemImage.rectTransform.sizeDelta = Vector2.zero;
										SetMouseItemText("");
									}
								}
							}
						}
					}
				}
				else if (InventoryOpen || FrameList[i].name.Contains("hotbar"))
				{
					FrameList[i].GetComponent<Frame>().SetSelected(false);
				}
			}
		}
		if (!InventoryOpen)
		{
			point.anchoredPosition = Vector3.LerpUnclamped(point.anchoredPosition, DefaultPointPosition + (Vector2.right * PointIncrement * SlotEquipped), 20 * Time.deltaTime);
			point.localScale = Vector3.one;
			point.rotation = Quaternion.identity;
		}
		else
		{
			point.anchoredPosition = DefaultPointPosition + (Vector2.right * PointIncrement * SlotEquipped) + Vector2.up * 63;
			point.localScale = Vector3.one * 1.8f;
			point.rotation = Quaternion.Euler(0, 0, Time.time * 100);
		}
	}

	private string FindToolType(string tool)
	{
		if (!tool.Contains("blueprint") && !tool.Contains("skill"))
		{
			if (tool.Contains("axe"))
			{
				return "axe";
			}
			else if (tool.Contains("pick"))
			{
				return "pick";
			}
			else if (tool.Contains("sword"))
			{
				return "sword";
			}
			else if (tool.Contains("air"))
			{
				return "nothing";
			}
			else
			{
				return tool;
			}
		}
		else
		{
			return tool;
		}
			
	}

	private void TryReplaceTool()
	{
		if (OldSlotEquipped != SlotEquipped)
		{
			UnityEngine.Object tool = ToolParent.GetChild(0);

			if (FindToolType(HotbarFrames[SlotEquipped].GetComponent<Frame>().GetItem()) != FindToolType(HotbarFrames[OldSlotEquipped].GetComponent<Frame>().GetItem()))
			{
				for (int i = 0; i < ToolParent.childCount; i++)
				{
					Destroy(ToolParent.GetChild(i).gameObject);
				}
				tool = Instantiate(Resources.Load($"Tool Prefabs/{FindToolType(HotbarFrames[SlotEquipped].GetComponent<Frame>().GetItem())}"), ToolParent, false);
			}
			tool.GetComponentsInChildren<SpriteRenderer>()[0].sprite = Resources.Load<Sprite>($"Item Images/{HotbarFrames[SlotEquipped].GetComponent<Frame>().GetItem()}");
			PlayerAnimator.Rebind();
			PlayerAnimator.Update(0f);
		}
	}

    private void ForceReplaceTool()
    {

        UnityEngine.Object tool = ToolParent.GetChild(0);

        if (FindToolType(HotbarFrames[SlotEquipped].GetComponent<Frame>().GetItem()) != FindToolType(HotbarFrames[OldSlotEquipped].GetComponent<Frame>().GetItem()))
        {
            for (int i = 0; i < ToolParent.childCount; i++)
            {
                Destroy(ToolParent.GetChild(i).gameObject);
            }
            tool = Instantiate(Resources.Load($"Tool Prefabs/nothing"), ToolParent, false);
        }
        tool.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>($"Item Images/nothing");
        PlayerAnimator.Rebind();
        PlayerAnimator.Update(0f);
    }

    private void TryReplaceToolSwap(string item)
	{
		UnityEngine.Object tool = ToolParent.GetChild(0);
        if (FindToolType(item) != FindToolType(tool.name))
		{
			for (int i = 0; i < ToolParent.childCount; i++)
			{
				Destroy(ToolParent.GetChild(i).gameObject); 
			}
			tool = Instantiate(Resources.Load($"Tool Prefabs/{FindToolType(item)}"), ToolParent, false);
		}
		tool.GetComponentsInChildren<SpriteRenderer>()[0].sprite = Resources.Load<Sprite>($"Item Images/{item}");
		PlayerAnimator.Rebind();
		PlayerAnimator.Update(0f);
	}

	private void SetMouseItem(Image Frame)
	{
		Image MouseItemImage = MouseItem.GetComponentsInChildren<Image>()[0];
		MouseItemImage.sprite = Frame.GetComponent<Frame>().GetSprite();
		MouseItem.transform.position = Frame.transform.position + new Vector3(16, 8);
		MouseItemImage.transform.rotation = Frame.GetComponent<Frame>().GetItemDisplay().transform.rotation;
		Frame.GetComponent<Frame>().SetItem("air");
		SelectedItemAmount = Frame.GetComponent<Frame>().GetItemAmount();
		if (SelectedItemAmount > 1)
		{
			SetMouseItemText(SelectedItemAmount.ToString());
		}
		else
		{
			SetMouseItemText("");
		}
		Frame.GetComponent<Frame>().SetItemAmount(0);
		MouseItemImage.SetNativeSize();
	}

	private void SetMouseItemText(string Text)
	{
		MouseItemText.text = Text;
		MouseItemBorder.text = Text;
	}    

	public void AddToFrameList(Image frame)
	{
		FrameList.Add(frame);
	}

    public void AddToObjectList(Object ob)
    {
        Objects.Add(ob);
    }

	public void QueueItemAdd(string item, float amount)
	{
		if (!ItemsQueued.Contains(item))
		{
            ItemsQueued.Add(item);
            ItemQueuedAmts.Add((int)amount);
		}
		else
		{
            ItemQueuedAmts[ItemsQueued.IndexOf(item)] += (int)amount;
        }
	}

	public string GetItemHolding()
	{
		return HotbarFrames[SlotEquipped].GetComponent<Frame>().GetItem();
    }

	public string GetItemName()
	{ 
		return HotbarFrames[SlotEquipped].GetComponent<Frame>().GetItem();
    }

	public void ChangeItemHoldingAmt(int amt)
	{
		HotbarFrames[SlotEquipped].GetComponent<Frame>().SetItemAmount(HotbarFrames[SlotEquipped].GetComponent<Frame>().GetItemAmount() + amt);
    }

	public float ItemHoldingDamage()
	{
		return HotbarFrames[SlotEquipped].GetComponent<Frame>().GetDamage();
	}

	public Frame GetFrameComponent(int index)
	{
		return FrameList[index].GetComponent<Frame>();
	}

    public ItemList GetGlobalItemList()
    { return GlobalItemList; }

	public void SetDamageAdd(float damage)
	{
		DamageAdd = damage;
	}
}
