namespace CompetitionResults.Data
{
    public class CompetitionStateService
    {
        private int _selectedCompetitionId;

        public int SelectedCompetitionId
        {
            get => _selectedCompetitionId;
            set
            {
                if (_selectedCompetitionId != value)
                {
                    _selectedCompetitionId = value;
                    NotifyCompetitionChanged();
                }
            }
        }

        public event Action OnCompetitionChanged;

        public void NotifyCompetitionChanged() => OnCompetitionChanged?.Invoke();
    }

}
