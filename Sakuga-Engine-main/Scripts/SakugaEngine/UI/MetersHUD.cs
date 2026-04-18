using Godot;

namespace SakugaEngine.UI
{
	public partial class MetersHUD : Control
	{
		[Export] private TextureProgressBar P1Contract;
		[Export] private TextureProgressBar P2Contract;
		[Export] private TextureProgressBar P1Seal;
		[Export] private TextureProgressBar P2Seal;
		[Export] private TextureProgressBar P1Charge;
		[Export] private TextureProgressBar P2Charge;
		[Export] private Label P1TrainingInfo;
		[Export] private Label P2TrainingInfo;
		[Export] private InputHistory P1InputHistory;
		[Export] private InputHistory P2InputHistory;

		private int CurrentFrameAdvantage;

		/*public override void _Ready()
		{
			P1Meter = GetNode<TextureProgressBar>("Meters/P1Meter");
			P2Meter = GetNode<TextureProgressBar>("Meters/P2Meter");
			P1TrainingInfo = GetNode<Label>("TrainingInfo/P1Info/Information");
			P2TrainingInfo = GetNode<Label>("TrainingInfo/P2Info/Information");
		}*/

		public void Setup(SakugaFighter[] fighters)
		{
			P1Contract.MaxValue = fighters[0].Variables.ExtraVariables[5].MaxValue;
			P2Contract.MaxValue = fighters[1].Variables.ExtraVariables[5].MaxValue;
			
			P1Seal.MaxValue = fighters[0].Variables.ExtraVariables[6].MaxValue;
			P2Seal.MaxValue = fighters[1].Variables.ExtraVariables[6].MaxValue;
		}

		public void UpdateMeters(SakugaFighter[] fighters)
		{
			// Contract Gauge
			P1Contract.Value = fighters[0].Variables.ExtraVariables[5].CurrentValue;
			P2Contract.Value = fighters[1].Variables.ExtraVariables[5].CurrentValue;
			
			// Seal Gauge
			P1Seal.Value = fighters[0].Variables.ExtraVariables[6].CurrentValue;
			P2Seal.Value = fighters[1].Variables.ExtraVariables[6].CurrentValue;
			
			// Charge Gauge
			int Charge1Value = fighters[0].Variables.ExtraVariables[4].CurrentValue;
			int Charge2Value = fighters[1].Variables.ExtraVariables[4].CurrentValue;
			
			if (Charge1Value < fighters[0].Variables.ExtraVariables[4].MaxValue)
				P1Charge.TextureProgress = P1ChargeChargeTexture;
			else if (Charge1Value >= fighters[0].Variables.ExtraVariables[4].MaxValue)
				P1Charge.TextureProgress = P1ChargeFullTexture;
				
			if (Charge2Value < fighters[1].Variables.ExtraVariables[4].MaxValue)
				P2Charge.TextureProgress = P2ChargeChargeTexture;
			else if (Charge2Value >= fighters[1].Variables.ExtraVariables[4].MaxValue)
				P2Charge.TextureProgress = P2ChargeFullTexture;
			
			P1Charge.Value = Charge1Value;
			P2Charge.Value = Charge2Value;
			
			GetFrameAdvantage(fighters);

			P1InputHistory.SetHistoryList(fighters[0].Inputs);
			P2InputHistory.SetHistoryList(fighters[1].Inputs);

			P1TrainingInfo.Text = TrainingInfoText(fighters[0], fighters[1]);
			P2TrainingInfo.Text = TrainingInfoText(fighters[1], fighters[0]);
		}

		void GetFrameAdvantage(SakugaFighter[] fighters)
		{
			for (int i = 0; i < fighters.Length; i++)
			{
				if (fighters[i].Tracker.FrameAdvantage != 0)
					CurrentFrameAdvantage = fighters[i].Tracker.FrameAdvantage;
			}
		}

		private string TrainingInfoText(SakugaFighter owner, SakugaFighter reference)
		{
			string hitTypeText = "";

			switch (reference.Tracker.LastHitType)
			{
				case 0:
					hitTypeText = "HIGH";
					break;
				case 1:
					hitTypeText = "MID";
					break;
				case 2:
					hitTypeText = "LOW";
					break;
				case 3:
					hitTypeText = "UNBLOCKABLE";
					break;
			}

			int finalFrameAdv = owner.Tracker.FrameAdvantage;// != 0 ? CurrentFrameAdvantage : CurrentFrameAdvantage;

			string frameAdvantageInfo = finalFrameAdv >= 0 ?
					("+" + finalFrameAdv) : "" + finalFrameAdv;

			string frameAdvText = "(" + frameAdvantageInfo + ")";

			FighterVariables vars = reference.Variables as FighterVariables;

			return reference.Tracker.LastDamage + "\n" +
					reference.Tracker.CurrentCombo + "\n" +
					reference.Tracker.HighestCombo + "\n" +
					hitTypeText + "\n" +
					vars.CurrentDamageScaling + "%\n" +
					owner.Tracker.FrameData + frameAdvText;
		}
	}
}
