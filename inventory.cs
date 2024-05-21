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
	private Image[] FrameList = new Image[10000];
	private Object[] Objects = new Object[10000];
	private Image SelectedFrame;
	private int SelectedFrameIndex;
	private int FrameSelectedItemIndex = 0;
	private int SelectedItemAmount;
	private int FrameIndex = 0;
	private int ObjectIndex = 0;
	private int[] ItemsQueued = new int[0];
	private int[] ItemQueuedAmts = new int[0];

	public static Inventory Instance { get; private set; }

	private void Start()
	{
		Instance = this;
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
		InstantiateObject("crafting bench", new Vector2(0, -2.5f), 0, true);
		for (int i = 0; i < 2; i++)
		{
			InstantiateObject("scrap wood", new Vector2(Random.Range(-2.5f, 2.5f), Random.Range(-2f, 2f)), 0);
		}
        for (int i = 0; i < 1; i++)
        {
            InstantiateObject("rock", new Vector2(Random.Range(-2.5f, 2.5f), Random.Range(-2f, 2f)), 0);
        }
        InstantiateObject("glowstar bush", new Vector2(Random.Range(-2.5f, 2.5f), Random.Range(-2f, 2f)), 0);
        //InstantiateObject("elevator", new Vector2(0, 3.1f), 0);
        //InstantiateObject("furnace", new Vector2(3, -2.5f), 0, true);
        //InstantiateObject("chest", new Vector2(-3, -2.5f), 0, true);
    }

	private void InstanstiateFrames()
	{
		for (int y = 0; y < 5; y++)
		{
			for (int x = 0; x < 5; x++)
			{
				Image f = Instantiate(Frame, transform);
				f.rectTransform.anchoredPosition = new Vector2(-67 + x * 75, f.rectTransform.anchoredPosition.y);
				if (y > 0)
				{
					f.rectTransform.anchoredPosition = new Vector2(f.rectTransform.anchoredPosition.x, 160 - 75 * y);
				}
				else
				{
					f.rectTransform.anchoredPosition = new Vector2(f.rectTransform.anchoredPosition.x, 180);
				}
				FrameList[FrameIndex] = f;
				FrameIndex += 1;
			}
		}
		SetFrame(0, 2, 1);
		SetFrame(1, 6, 1);
		SetFrame(2, 1, 99);
		SetFrame(3, 7, 99);
		SetFrame(4, 5, 1);
		SetFrame(5, 8, 1);
		SetFrame(6, 9, 1);
		SetFrame(7, 11, 1);
		SetFrame(8, 12, 1);
		SetFrame(9, 13, 1);
		SetFrame(10, 14, 1);
        SetFrame(11, 15, 100);
        SetFrame(12, 18, 100);
        SetFrame(13, 16, 100);
    }

	private void InstantiateObject(string ObjectName, Vector3 Position, int index, bool HasFrame = false)
	{
		Transform Object = Instantiate(Resources.Load<Transform>($"Object Prefabs/{ObjectName}"), Position, Quaternion.identity, RoomManager.GetRoom(true));
		Objects[ObjectIndex] = Object.GetComponent<Object>();
		Object.GetComponent<Object>().SetInventory(GetComponent<Inventory>());
		if (HasFrame)
		{
			for (int i = 0; i < Object.GetComponentsInChildren<Image>().Length; i++)
			{ 
				if (Object.GetComponentsInChildren<Image>()[i].name.Contains("Frame"))
				{
					FrameList[FrameIndex] = Object.GetComponentsInChildren<Image>()[i];
					FrameIndex += 1;
				}
			}
		}
		ObjectIndex += 1;
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
        }
	}

	private void Update()
	{
		for (int i = 0; i < 100; i++)
		{
			if (Objects[i] != null)
			{
				Objects[i].SetSelectedFrameIndex(SelectedFrameIndex);
				Objects[i].SetItemName(FrameList[SlotEquipped].GetComponent<Frame>().GetItem());
				if (Objects[i].GetDurability() <= 0)
				{
					if (Objects[i].name.Contains("scrap wood"))
					{
						QueueItemAdd(7, 4);
						if (Random.Range(0,3) < 2)
						{
                            InstantiateObject("scrap wood", new Vector2(Random.Range(-3f, 3f), Random.Range(-2.5f, 2f)), 0);
                        }
                        else
						{
                            InstantiateObject("rock", new Vector2(Random.Range(-3f, 3f), Random.Range(-2.5f, 2f)), 0);
                        }
                    }
					if (Objects[i].name.Contains("rock"))
					{
						QueueItemAdd(10, 1);
                        if (Random.Range(0, 3) < 2)
                        {
                            InstantiateObject("scrap wood", new Vector2(Random.Range(-3f, 3f), Random.Range(-2.5f, 2f)), 0);
                        }
                        else
                        {
                            InstantiateObject("rock", new Vector2(Random.Range(-3f, 3f), Random.Range(-2.5f, 2f)), 0);
                        }
                    }
					if (Objects[i].name.Contains("glowstar bush"))
					{
                        QueueItemAdd(14, 3);
                        InstantiateObject("glowstar bush", new Vector2(Random.Range(-2.5f, 2.5f), Random.Range(-2.5f, 2f)), 0);
                    }
                    if (Objects[i].name.Contains("raw iron"))
                    {
                        QueueItemAdd(16, 2);
                    }
                    if (Objects[i].name.Contains("furnace"))
                    {
                        QueueItemAdd(18, 1);
                    }
                    if (Objects[i].name.Contains("crafting bench"))
                    {
                        QueueItemAdd(15, 1);
                    }
                    Destroy(Objects[i].gameObject);
                }
			}
		}
		Image MouseItemImage = MouseItem.GetComponentsInChildren<Image>()[0];
		MouseItem.transform.position = Vector2.LerpUnclamped(MouseItem.transform.position, Input.mousePosition, 35 * Time.deltaTime);
		if (!FrameList[SlotEquipped].GetComponent<Frame>().GetItem().Contains("blueprint"))
		{
			if (FrameList[SlotEquipped].GetComponent<Frame>().GetItem().Contains("axe"))
			{
				PlayerAnimator.SetInteger("tool", 2);
			}
			else if (FrameList[SlotEquipped].GetComponent<Frame>().GetItem().Contains("pick"))
			{
				PlayerAnimator.SetInteger("tool", 3);
			}
			else if (FrameList[SlotEquipped].GetComponent<Frame>().GetItem().Contains("sword") | FrameList[SlotEquipped].GetComponent<Frame>().GetItem().Contains("stick") | FrameList[SlotEquipped].GetComponent<Frame>().GetItem().Contains("stone"))
			{
				PlayerAnimator.SetInteger("tool", 4);
			}
		}
		else
		{
			PlayerAnimator.SetInteger("tool", 0);
		}
		for (int i = 0; i < 100; i++)
		{
			if (FrameList[i] != null)
			{
				FrameList[i].GetComponent<Frame>().SetInventoryOpen(InventoryOpen);
				if (ItemsQueued.Length != 0)
				{
						for (int j = 0; j < ItemsQueued.Length; j++) 
						{
							if (FrameList[i].GetComponent<Frame>().GetItemIndex() == 0 | FrameList[i].GetComponent<Frame>().GetItemIndex() == ItemsQueued[j])
							{
								FrameList[i].GetComponent<Frame>().SetItemIndex(ItemsQueued[j]);
								FrameList[i].GetComponent<Frame>().SetItemAmount(ItemQueuedAmts[j] + FrameList[i].GetComponent<Frame>().GetItemAmount());
								ItemsQueued = ArrayRemoveAt(ItemsQueued, j);
								ItemQueuedAmts = ArrayRemoveAt(ItemQueuedAmts, j);
							}
						}
				}
				if (InventoryOpen)
				{
					if (i < 5)
					{
						FrameList[i].GetComponent<Frame>().SetSelectedOffset(-11);
					}
					else if (i < 25)
					{
						FrameList[i].GetComponent<Frame>().SetSelectedOffset(5);
					}
				}
				else
				{
					if (i < 5)
					{
						FrameList[i].GetComponent<Frame>().SetSelectedOffset(-11);
					}
					else if (i < 25)
					{
						FrameList[i].GetComponent<Frame>().SetSelectedOffset(-40);
						FrameList[i].GetComponent<Frame>().SetSelected(true);
					}
				}
				if (SlotEquipped == i)
				{
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
							if (FrameSelectedItemIndex == 0)
							{
								if (FrameList[i].GetComponent<Frame>().GetItemIndex() != 0)
								{
									FrameSelectedItemIndex = FrameList[i].GetComponent<Frame>().GetItemIndex();
									SetMouseItem(FrameList[i]);
									SelectedFrame = FrameList[i];
									SelectedFrameIndex = i;
									TryReplaceToolSwap(i, FrameList[i].GetComponent<Frame>().GetItemIndex());
								}
							}
							else
							{
								if (FrameSelectedItemIndex == FrameList[i].GetComponent<Frame>().GetItemIndex())
								{
									FrameList[i].GetComponent<Frame>().SetSelected(false);
									FrameList[i].GetComponent<Frame>().SetItemIndex(FrameSelectedItemIndex);
									FrameList[i].GetComponent<Frame>().SetItemAmount(SelectedItemAmount + FrameList[i].GetComponent<Frame>().GetItemAmount());
									FrameSelectedItemIndex = 0;
									MouseItemImage.rectTransform.sizeDelta = Vector2.zero;
									SetMouseItemText("");
								}
								else if (!IsResult | FrameList[i].GetComponent<Frame>().GetItemIndex() == 0)
								{
									if (!IsResult)
									{
										SelectedFrame.GetComponent<Frame>().SetItemIndex(FrameList[i].GetComponent<Frame>().GetItemIndex());
										SelectedFrame.GetComponent<Frame>().SetItemAmount(FrameList[i].GetComponent<Frame>().GetItemAmount());
									}
									SelectedFrame.GetComponent<Frame>().SetItemIndex(FrameList[i].GetComponent<Frame>().GetItemIndex());
									SelectedFrame.GetComponent<Frame>().SetItemAmount(FrameList[i].GetComponent<Frame>().GetItemAmount());
									TryReplaceToolSwap(i, FrameSelectedItemIndex);
									FrameList[i].GetComponent<Frame>().SetSelected(false);
									FrameList[i].GetComponent<Frame>().SetItemIndex(FrameSelectedItemIndex);
									FrameList[i].GetComponent<Frame>().SetItemAmount(SelectedItemAmount);
									FrameSelectedItemIndex = 0;
									MouseItemImage.rectTransform.sizeDelta = Vector2.zero;
									SetMouseItemText("");
								}
							}
						}
					}
				}
				else if (FrameList[i].GetComponent<Frame>().TouchingMouse())
				{
					//print(i);
					if (InventoryOpen)
					{
						FrameList[i].GetComponent<Frame>().SetSelected(true);
					}
					if (Input.GetMouseButtonDown(1))
					{
						if (FrameSelectedItemIndex == 0)
						{
							if (FrameList[i].GetComponent<Frame>().GetItemIndex() != 0)
							{
								FrameSelectedItemIndex = FrameList[i].GetComponent<Frame>().GetItemIndex();
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
                            if (FrameSelectedItemIndex != 0 && FrameList[i].GetComponent<Frame>().GetAcceptableItems().Contains(FrameSelectedItemIndex))
							{
								if (FrameSelectedItemIndex == FrameList[i].GetComponent<Frame>().GetItemIndex())
								{
									FrameList[i].GetComponent<Frame>().SetSelected(false);
									FrameList[i].GetComponent<Frame>().SetItemIndex(FrameSelectedItemIndex);
									FrameList[i].GetComponent<Frame>().SetItemAmount(SelectedItemAmount + FrameList[i].GetComponent<Frame>().GetItemAmount());
								}
								else
								{
									SelectedFrame.GetComponent<Frame>().SetItemIndex(FrameList[i].GetComponent<Frame>().GetItemIndex());
									SelectedFrame.GetComponent<Frame>().SetItemAmount(FrameList[i].GetComponent<Frame>().GetItemAmount());
									if (SelectedFrameIndex != SlotEquipped && FrameList[i].GetComponent<Frame>().GetItemIndex() != 0)
									{
										TryReplaceToolSwap(i, FrameList[i].GetComponent<Frame>().GetItemIndex());
									}
									FrameList[i].GetComponent<Frame>().SetSelected(false);
									FrameList[i].GetComponent<Frame>().SetItemIndex(FrameSelectedItemIndex);
									FrameList[i].GetComponent<Frame>().SetItemAmount(SelectedItemAmount);
								}
								FrameSelectedItemIndex = 0;
								MouseItemImage.rectTransform.sizeDelta = Vector2.zero;
								SetMouseItemText("");
							}
						}
						else
						{
							if (FrameSelectedItemIndex != 0)
							{
								if (FrameSelectedItemIndex == FrameList[i].GetComponent<Frame>().GetItemIndex())
								{
									FrameList[i].GetComponent<Frame>().SetSelected(false);
									FrameList[i].GetComponent<Frame>().SetItemIndex(FrameSelectedItemIndex);
									FrameList[i].GetComponent<Frame>().SetItemAmount(SelectedItemAmount + FrameList[i].GetComponent<Frame>().GetItemAmount());
									FrameSelectedItemIndex = 0;
									MouseItemImage.rectTransform.sizeDelta = Vector2.zero;
									SetMouseItemText("");
								}
								else
								{
									if (!IsResult | FrameList[i].GetComponent<Frame>().GetItemIndex() == 0)
									{
										if (!IsResult)
										{
											SelectedFrame.GetComponent<Frame>().SetItemIndex(FrameList[i].GetComponent<Frame>().GetItemIndex());
											SelectedFrame.GetComponent<Frame>().SetItemAmount(FrameList[i].GetComponent<Frame>().GetItemAmount());
										}
											
										if (SelectedFrameIndex == SlotEquipped && FrameList[i].GetComponent<Frame>().GetItemIndex() != 0)
										{
											TryReplaceToolSwap(i, FrameList[i].GetComponent<Frame>().GetItemIndex());
										}
										FrameList[i].GetComponent<Frame>().SetSelected(false);
										FrameList[i].GetComponent<Frame>().SetItemIndex(FrameSelectedItemIndex);
										FrameList[i].GetComponent<Frame>().SetItemAmount(SelectedItemAmount);
										FrameSelectedItemIndex = 0;
										MouseItemImage.rectTransform.sizeDelta = Vector2.zero;
										SetMouseItemText("");
									}
								}
							}
						}
					}
				}
				else if (InventoryOpen || i < 5)
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

	private int[] ArrayRemoveAt(int[] array, int index)
	{
		var list = new List<int>(array);
		list.RemoveAt(index);
        return list.ToArray();
	}

	private string FindToolType(string tool)
	{
		if (!tool.Contains("blueprint"))
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

			if (FindToolType(FrameList[SlotEquipped].GetComponent<Frame>().GetItem()) != FindToolType(FrameList[OldSlotEquipped].GetComponent<Frame>().GetItem()))
			{
				for (int i = 0; i < ToolParent.childCount; i++)
				{
					Destroy(ToolParent.GetChild(i).gameObject);
				}
				tool = Instantiate(Resources.Load($"Tool Prefabs/{FindToolType(FrameList[SlotEquipped].GetComponent<Frame>().GetItem())}"), ToolParent, false);
			}
			tool.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>($"Item Images/{FrameList[SlotEquipped].GetComponent<Frame>().GetItem()}");
			PlayerAnimator.Rebind();
			PlayerAnimator.Update(0f);
		}
	}

	private void TryReplaceToolSwap(int index, int itemindex)
	{
		UnityEngine.Object tool = ToolParent.GetChild(0);
		if (FindToolType(FrameList[index].GetComponent<Frame>().GetItemListAtIndex(itemindex)) != FindToolType(tool.name))
		{
			for (int i = 0; i < ToolParent.childCount; i++)
			{
				Destroy(ToolParent.GetChild(i).gameObject); 
			}
			tool = Instantiate(Resources.Load($"Tool Prefabs/{FindToolType(FrameList[index].GetComponent<Frame>().GetItemListAtIndex(itemindex))}"), ToolParent, false);
		}
		tool.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>($"Item Images/{FrameList[index].GetComponent<Frame>().GetItemListAtIndex(itemindex)}");
		PlayerAnimator.Rebind();
		PlayerAnimator.Update(0f);
	}

	private void SetMouseItem(Image Frame)
	{
		Image MouseItemImage = MouseItem.GetComponentsInChildren<Image>()[0];
		MouseItemImage.sprite = Frame.GetComponent<Frame>().GetSprite();
		MouseItem.transform.position = Frame.transform.position + new Vector3(16, 8);
		MouseItemImage.transform.rotation = Frame.GetComponent<Frame>().GetItemDisplay().transform.rotation;
		Frame.GetComponent<Frame>().SetItemIndex(0);
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
		FrameList.Append(frame);
	}

	private void SetFrame(int Index, int ItemIndex, int Amount)
	{
		FrameList[Index].GetComponent<Frame>().SetItemIndex(ItemIndex);
		FrameList[Index].GetComponent<Frame>().SetItemAmount(Amount);
	}

	public void QueueItemAdd(int item, float amount)
	{
		var ItemsQueuedList = new List<int>(ItemsQueued.ToList());
		var ItemQueuedAmtsList = new List<int>(ItemQueuedAmts.ToList());
		if (!ItemsQueuedList.Contains(item))
		{
			ItemsQueuedList.Add(item);
			ItemQueuedAmtsList.Add((int)amount);
		}
		else
		{
			ItemQueuedAmtsList[ItemsQueuedList.IndexOf(item)] += (int)amount;
        }
		ItemsQueued = ItemsQueuedList.ToArray();
		ItemQueuedAmts = ItemQueuedAmtsList.ToArray();
	}

	public int GetItemHolding()
	{
		return FrameList[SlotEquipped].GetComponent<Frame>().GetItemIndex();
    }
}
