using Content.Shared._RedStar.Records;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RedStar.Records;

public sealed class CharacterDossierView : BoxContainer
{
    public CharacterDossierView(CharacterDossier dossier, DossierSection section)
    {
        Orientation = LayoutOrientation.Vertical;
        Margin = new Thickness(0, 12, 0, 0);
        AddChild(new Label { Text = Loc.GetString("character-dossier-section-title"), StyleClasses = { "LabelHeading" } });

        Add("character-dossier-emergency-contact", dossier.EmergencyContact);
        Add("character-dossier-residence", dossier.Residence);
        Add("character-dossier-identifying-features", dossier.IdentifyingFeatures);
        Add("character-dossier-general-notes", dossier.GeneralNotes);

        switch (section)
        {
            case DossierSection.Employment:
                Add("character-dossier-education", dossier.Education);
                Add("character-dossier-previous-employment", dossier.PreviousEmployment);
                Add("character-dossier-professional-experience", dossier.ProfessionalExperience);
                Add("character-dossier-employment-notes", dossier.EmploymentNotes);
                AddEntries(dossier.EmploymentEntries);
                break;
            case DossierSection.Medical:
                if (dossier.Weight > 0)
                    Add("character-dossier-weight", dossier.Weight.ToString());
                Add("character-dossier-allergies", dossier.Allergies);
                Add("character-dossier-drug-allergies", dossier.DrugAllergies);
                Add("character-dossier-medical-conditions", dossier.MedicalConditions);
                Add("character-dossier-prescriptions", dossier.Prescriptions);
                Add("character-dossier-postmortem", dossier.PostmortemInstructions);
                AddEntries(dossier.MedicalEntries);
                break;
            case DossierSection.Security:
                Add("character-dossier-security-background", dossier.SecurityBackground);
                Add("character-dossier-security-notes", dossier.SecurityNotes);
                AddEntries(dossier.SecurityEntries);
                break;
        }
    }

    private void Add(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;
        AddChild(new Label { Text = Loc.GetString(key, ("value", value)), ClipText = false });
    }

    private void AddEntries(List<DossierEntry> entries)
    {
        foreach (var entry in entries)
            AddChild(new Label { Text = $"{entry.Title}: {entry.Description}", ClipText = false });
    }
}
