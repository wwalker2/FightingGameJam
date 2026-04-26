using Godot;
using SakugaEngine.Resources;
using System.IO;

namespace SakugaEngine
{
	[GlobalClass]
	public partial class FighterVariables : SakugaVariables
	{
		public ushort CurrentAttack;
		public ushort CurrentDefense;
		public ushort CurrentDamageScaling;
		public ushort CurrentBaseDamageScaling;
		public ushort CurrentCornerDamageScaling;
		public ushort CurrentDamageProration;
		public ushort CurrentGravityProration;

		public uint PartnerMeter;
		public bool PartnerMeterFull;

		public byte Contracts;
		public byte Seals;
		
		public int LostHealth;

		public override void Initialize(SakugaActor owner)
		{
			base.Initialize(owner);
			LostHealth = CurrentHealth;

			CurrentAttack = _owner.Data.BaseAttack;
			CurrentDefense = _owner.Data.BaseDefense;

			CurrentBaseDamageScaling = Global.BaseMaxDamageScaling;
			CurrentCornerDamageScaling = Global.CornerMaxDamageScaling;
			CurrentDamageScaling = CurrentBaseDamageScaling;

			PartnerMeter = 0;

			CurrentDamageProration = 100;
			CurrentGravityProration = 100;

			ResetContracts();
		}

		public void AddContract()
		{
			if (Contracts < _owner.Data.MaxContracts - Seals)
				Contracts++;

			Contracts = (byte)Mathf.Clamp(Contracts, 0, _owner.Data.MaxContracts - Seals);
		}

		public void SpendContract(int amount)
		{
			for (int i = 0; i < amount; i++)
			{
				SpendContract();
			}
		}

		public void SpendContract()
		{
			if (ContractsSealed()) return;

			if (Contracts > 0)
			{
				Contracts--;
			}
			else
			{
				Seals++;
			}

			Seals = (byte)Mathf.Clamp(Seals, 0, _owner.Data.MaxContracts);
			Contracts = (byte)Mathf.Clamp(Contracts, 0, _owner.Data.MaxContracts - Seals);
			
		}

		public void RecoverContract()
		{
			Seals--;
			Contracts++;

			Seals = (byte)Mathf.Clamp(Seals, 0, _owner.Data.MaxContracts);
			Contracts = (byte)Mathf.Clamp(Contracts, 0, _owner.Data.MaxContracts - Seals);
		}

		public bool CanUseContracts(byte amount)
		{
			return Seals < amount;
		}

		public bool ContractsSealed()
		{
			return Seals >= _owner.Data.MaxContracts;
		}


		public void ResetContracts()
		{
			Contracts = 1;
			Seals = 0;
		}

		public override void TakeDamage(int damage, int meterGain, bool isKilingBlow)
		{
			base.TakeDamage(damage, meterGain, isKilingBlow);
			if (CurrentHealth == 0)
				LostHealth = 0;
		}

		public void RemoveDamageScaling(ushort value)
		{
			if (CurrentBaseDamageScaling - value < Global.BaseMinDamageScaling)
				CurrentBaseDamageScaling = Global.BaseMinDamageScaling;
			else CurrentBaseDamageScaling -= value;

			if (CurrentCornerDamageScaling - value < Global.CornerMinDamageScaling)
				CurrentCornerDamageScaling = Global.CornerMinDamageScaling;
			else CurrentCornerDamageScaling -= value;
		}

		public void ResetDamageStatus()
		{
			CurrentBaseDamageScaling = Global.BaseMaxDamageScaling;
			CurrentCornerDamageScaling = Global.CornerMaxDamageScaling;
			CurrentDamageProration = 100;
			CurrentGravityProration = 100;
			UpdateLostHealth();
		}

		public void UpdateLostHealth()
		{
			if (LostHealth > CurrentHealth)
				LostHealth -= _owner.Data.MaxHealth / 200;
			else if (LostHealth < CurrentHealth)
				LostHealth = CurrentHealth;
		}

		public void CalculateDamageScaling(bool changeCondition)
		{
			if (changeCondition)
				CurrentDamageScaling = CurrentCornerDamageScaling;
			else
				CurrentDamageScaling = CurrentBaseDamageScaling;
		}

		public int CalculateCompleteDamage(int damage, int attackValue)
		{
			var damageFactor = attackValue - (CurrentDefense - 100);
			var scaledDamage = damage * CurrentDamageScaling / 100;
			return scaledDamage * damageFactor / 100;
		}

		public override void Serialize(BinaryWriter bw)
		{
			base.Serialize(bw);

			bw.Write(LostHealth);
			bw.Write(CurrentAttack);
			bw.Write(CurrentDefense);
			bw.Write(CurrentDamageScaling);
			bw.Write(CurrentBaseDamageScaling);
			bw.Write(CurrentCornerDamageScaling);
			bw.Write(CurrentDamageProration);
			bw.Write(CurrentGravityProration);

			bw.Write(PartnerMeter);
			bw.Write(PartnerMeterFull);

			bw.Write(Contracts);
			bw.Write(Seals);
		}

		public override void Deserialize(BinaryReader br)
		{
			base.Deserialize(br);

			LostHealth = br.ReadInt32();
			CurrentAttack = br.ReadUInt16();
			CurrentDefense = br.ReadUInt16();
			CurrentDamageScaling = br.ReadUInt16();
			CurrentBaseDamageScaling = br.ReadUInt16();
			CurrentCornerDamageScaling = br.ReadUInt16();
			CurrentDamageProration = br.ReadUInt16();
			CurrentGravityProration = br.ReadUInt16();
			PartnerMeter = br.ReadUInt32();
			PartnerMeterFull = br.ReadBoolean();
			Contracts = br.ReadByte();
			Seals = br.ReadByte();
		}
	}
}
