# Generates Patina/Strings/{en,fr}/Resources.resw from one table so the two languages never drift.
import re, glob, json, sys, html
import os
root = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'Patina')

T = {
# x:Uid strings: key -> (en, fr)
"Map_OpenRecord.Content": ("Open record", "Ouvrir la fiche"),
"Map_CloseCallout.[using:Microsoft.UI.Xaml.Automation]AutomationProperties.Name": ("Close", "Fermer"),
"Artwork_NoPhotoTitle.Text": ("No photograph yet", "Aucune photographie"),
"Artwork_NoPhotoBody.Text": ("Photos added during a survey become the record photograph.", "Les photos ajoutées pendant une inspection deviennent la photographie de la fiche."),
"Artwork_ConditionHeading.Text": ("Condition", "État"),
"Artwork_LastSurveyed.Text": ("Last surveyed", "Dernière inspection"),
"Artwork_OpenTreatments.Text": ("Open treatments", "Traitements en cours"),
"Artwork_NoOpenTreatments.Text": ("Nothing open. Serious and urgent findings become treatments when a survey is submitted.", "Rien en cours. Les constats sérieux et urgents deviennent des traitements à la remise d'une inspection."),
"Artwork_History.Text": ("Condition history", "Historique de l'état"),
"Artwork_NoHistory.Text": ("Never surveyed. The first survey sets the grade and the survey schedule.", "Jamais inspectée. La première inspection fixe la cote et le calendrier d'inspection."),
"Artwork_CompletedTreatments.Text": ("Completed treatments", "Traitements terminés"),
"Artwork_GoneTitle.Text": ("This record is no longer in the collection", "Cette fiche n'est plus dans la collection"),
"Artwork_GoneBody.Text": ("It may have been removed by an import. Go back to the collection.", "Elle a peut-être été retirée par une importation. Retournez à la collection."),
"Artwork_ErrorTitle.Text": ("This record could not be opened", "Impossible d'ouvrir cette fiche"),
"Common_Retry.Content": ("Try again", "Réessayer"),
"Collection_DraftBadge.Text": ("Draft in progress", "Brouillon en cours"),
"Collection_Eyebrow.Text": ("City public art collection", "Collection d'art public de la Ville"),
"Collection_NeedAttention.Text": ("need attention", "à surveiller"),
"Collection_ShowMap.Text": ("Map", "Carte"),
"Collection_Search.PlaceholderText": ("Search title, artist, accession or place", "Chercher un titre, un artiste, un numéro ou un lieu"),
"Collection_NoMatchTitle.Text": ("No works match", "Aucune œuvre trouvée"),
"Collection_NoMatchBody.Text": ("Nothing in the collection matches this search and filter.", "Rien dans la collection ne correspond à cette recherche et à ce filtre."),
"Collection_ClearFilters.Content": ("Show the whole collection", "Afficher toute la collection"),
"Collection_ErrorTitle.Text": ("The collection could not be opened", "Impossible d'ouvrir la collection"),
"Main_Wordmark.Text": ("Patina", "Patina"),
"Nav_Collection.Content": ("Collection", "Collection"),
"Nav_Queue.Content": ("Treatments", "Traitements"),
"Nav_Settings.Content": ("Settings", "Réglages"),
"Map_Title.Content": ("Collection map", "Carte de la collection"),
"Queue_Late.Text": ("Late", "En retard"),
"Queue_Eyebrow.Text": ("Treatment queue", "File des traitements"),
"Queue_Urgent.Text": ("urgent", "urgents"),
"Queue_EmptyTitle.Text": ("Nothing here", "Rien ici"),
"Queue_EmptyBody.Text": ("No treatments in this status. Submitted surveys propose treatments for serious and urgent findings.", "Aucun traitement dans cet état. Les inspections remises proposent des traitements pour les constats sérieux et urgents."),
"Queue_ShowOpen.Content": ("Show all open work", "Afficher tout le travail en cours"),
"Queue_ErrorTitle.Text": ("The queue could not be opened", "Impossible d'ouvrir la file"),
"Settings_Eyebrow.Text": ("Settings", "Réglages"),
"Settings_SurveyorHeading.Text": ("Who is surveying?", "Qui inspecte?"),
"Settings_SurveyorBody.Text": ("Your name is stamped on every survey and log entry you make on this device.", "Votre nom figure sur chaque inspection et chaque entrée de journal faites sur cet appareil."),
"Settings_Surveyor.Header": ("Name", "Nom"),
"Settings_AppearanceHeading.Text": ("Appearance", "Apparence"),
"Settings_Theme.Header": ("Theme", "Thème"),
"Settings_ReduceMotion.Header": ("Reduce motion", "Réduire les animations"),
"Settings_LanguageHeading.Text": ("Language", "Langue"),
"Settings_DataHeading.Text": ("Collection data", "Données de la collection"),
"Settings_DataBody.Text": ("The collection lives on this device. Export it to a .patina file to back it up or move it to another device; importing replaces everything here.", "La collection est conservée sur cet appareil. Exportez-la dans un fichier .patina pour la sauvegarder ou la transférer; une importation remplace tout le contenu actuel."),
"Settings_Export.Content": ("Export collection", "Exporter la collection"),
"Settings_Import.Content": ("Import collection", "Importer une collection"),
"Settings_Reset.Content": ("Restore sample collection", "Restaurer la collection d'exemple"),
"Settings_AboutHeading.Text": ("About", "À propos"),
"Settings_AboutSample.Text": ("The sample collection is fictional: invented works and artists placed at real Montréal sites.", "La collection d'exemple est fictive : des œuvres et des artistes inventés, placés sur de vrais sites de Montréal."),
"Settings_AboutCredits.Text": ("Map data © OpenStreetMap contributors (ODbL). IBM Plex typefaces under the SIL Open Font License.", "Données cartographiques © les contributeurs d'OpenStreetMap (ODbL). Polices IBM Plex sous licence SIL Open Font License."),
"Settings_DataFolder.Text": ("Data folder", "Dossier des données"),
"Survey_Title.Content": ("Condition survey", "Inspection de l'état"),
"Survey_Back.Label": ("Back", "Retour"),
"Survey_GradeHeading.Text": ("Grade from findings", "Cote selon les constats"),
"Survey_GradeRule.Text": ("The worst finding sets the grade. Three moderate findings count as Poor.", "Le constat le plus grave fixe la cote. Trois constats modérés comptent comme Mauvais."),
"Survey_DetailsHeading.Text": ("Survey", "Inspection"),
"Survey_Surveyor.Header": ("Surveyor", "Inspecteur ou inspectrice"),
"Survey_WeatherLabel.Text": ("Weather", "Météo"),
"Survey_Notes.Header": ("General notes", "Notes générales"),
"Survey_OverviewPhotos.Text": ("Overview photographs", "Photographies d'ensemble"),
"Survey_AddPhoto.Text": ("Add photo", "Ajouter une photo"),
"Survey_TakePhoto.Text": ("Take photo", "Prendre une photo"),
"Survey_FindingsHeading.Text": ("Findings", "Constats"),
"Survey_NoFindings.Text": ("No findings yet. A survey with no findings submits as Good.", "Aucun constat pour l'instant. Une inspection sans constat est remise avec la cote Bon."),
"Survey_RemoveFinding.Content": ("Remove", "Retirer"),
"Survey_AddFindingHeading.Text": ("Add a finding", "Ajouter un constat"),
"Survey_FindingTypeLabel.Text": ("Type", "Type"),
"Survey_FindingType.PlaceholderText": ("Choose a type", "Choisir un type"),
"Survey_FindingZone.Header": ("Where on the object", "Endroit sur l'œuvre"),
"Survey_FindingZone.PlaceholderText": ("North face, base, left hand…", "Face nord, socle, main gauche…"),
"Survey_FindingSeverity.Header": ("Severity", "Gravité"),
"Survey_FindingNote.Header": ("Note", "Note"),
"Survey_AddFinding.Content": ("Add finding", "Ajouter le constat"),
"Survey_Submit.Content": ("Submit survey", "Remettre l'inspection"),
"Survey_SaveDraft.Content": ("Save draft", "Enregistrer le brouillon"),
"Survey_Discard.Content": ("Discard draft", "Supprimer le brouillon"),
"Treatment_Title.Content": ("Treatment", "Traitement"),
"Treatment_Origin.Text": ("From the survey of", "Issu de l'inspection du"),
"Treatment_LogHeading.Text": ("Log", "Journal"),
"Treatment_GoneTitle.Text": ("This treatment is no longer in the queue", "Ce traitement n'est plus dans la file"),
"Treatment_GoneBody.Text": ("It may have been removed by an import. Go back to the queue.", "Il a peut-être été retiré par une importation. Retournez à la file."),
"Treatment_ErrorTitle.Text": ("This treatment could not be opened", "Impossible d'ouvrir ce traitement"),
"Treatment_Date.Header": ("Scheduled for", "Prévu le"),
"Treatment_Assignee.Header": ("Assigned to", "Assigné à"),
"Treatment_Note.Header": ("Add to the log", "Ajouter au journal"),
"Treatment_Note.PlaceholderText": ("What was done, what was found. Required to mark the work done.", "Ce qui a été fait, ce qui a été constaté. Requis pour marquer le travail comme terminé."),
"Treatment_Save.Content": ("Save", "Enregistrer"),
"Treatment_StepBack.Content": ("Step back", "Revenir à l'étape précédente"),

# Code strings
"ApplicationName": ("Patina", "Patina"),
"TitleSeparator": (": ", " : "),
"Artwork_Interval": ("{0} works are surveyed every {1} months.", "Les œuvres en {0} sont inspectées tous les {1} mois."),
"Artwork_IntervalAdjusted": ("{0} works are surveyed every {1} months; at {2} this one is due every {3} months.", "Les œuvres en {0} sont inspectées tous les {1} mois; à la cote {2}, celle-ci l'est tous les {3} mois."),
"Artwork_ResumeSurvey": ("Resume draft survey", "Reprendre le brouillon d'inspection"),
"Artwork_StartSurvey": ("Start survey", "Commencer l'inspection"),
"Common_Cancel": ("Cancel", "Annuler"),
"Common_UnnamedSurveyor": ("Unnamed surveyor", "Inspecteur sans nom"),
"Due_Current": ("Next survey {0}", "Prochaine inspection le {0}"),
"Due_Never": ("Never surveyed", "Jamais inspectée"),
"Due_Overdue": ("Overdue since {0}", "En retard depuis le {0}"),
"Due_Soon": ("Due {0}", "À faire le {0}"),
"Grade_Accessible": ("Condition {0} of 4, {1}", "État {0} sur 4, {1}"),
"Log_Proposed": ("Proposed from a survey finding.", "Proposé à partir d'un constat d'inspection."),
"Log_StatusChanged": ("Moved to {0}.", "Passé à l'étape {0}."),
"Log_StatusReturned": ("Returned to {0}.", "Ramené à l'étape {0}."),
"Settings_ImportBody": ("Replace everything on this device with {0}? It holds {1} works and {2} surveys. Export first if you want to keep the current collection.", "Remplacer tout le contenu de cet appareil par {0}? Ce fichier contient {1} œuvres et {2} inspections. Exportez d'abord si vous voulez conserver la collection actuelle."),
"Settings_ImportConfirm": ("Replace collection", "Remplacer la collection"),
"Settings_ImportTitle": ("Import collection", "Importer une collection"),
"Settings_Imported": ("Collection imported.", "Collection importée."),
"Settings_Exported": ("Collection exported.", "Collection exportée."),
"Settings_ExportFailed": ("The file could not be written. Choose another location and try again.", "Impossible d'écrire le fichier. Choisissez un autre emplacement et réessayez."),
"Settings_LanguageRestart": ("Restart Patina to switch language.", "Redémarrez Patina pour changer de langue."),
"Settings_ResetBody": ("Every survey, treatment and photo reference on this device will be replaced by the sample collection. Export first to keep them.", "Toutes les inspections, tous les traitements et toutes les références de photos de cet appareil seront remplacés par la collection d'exemple. Exportez d'abord pour les conserver."),
"Settings_ResetConfirm": ("Restore sample", "Restaurer l'exemple"),
"Settings_ResetDone": ("Sample collection restored.", "Collection d'exemple restaurée."),
"Settings_ResetTitle": ("Restore the sample collection?", "Restaurer la collection d'exemple?"),
"Settings_ThemeDark": ("Dark", "Sombre"),
"Settings_ThemeLight": ("Light", "Clair"),
"Settings_ThemeSystem": ("System", "Système"),
"Survey_DiscardBody": ("The findings and notes in this draft will be deleted. Photos already taken stay on the device.", "Les constats et les notes de ce brouillon seront supprimés. Les photos déjà prises restent sur l'appareil."),
"Survey_DiscardConfirm": ("Discard", "Supprimer"),
"Survey_DiscardTitle": ("Discard this draft?", "Supprimer ce brouillon?"),
"Survey_DraftSaved": ("Draft saved.", "Brouillon enregistré."),
"Survey_FindingTypeRequired": ("Choose a type for the finding.", "Choisissez un type pour le constat."),
"Survey_PhotoFailed": ("The photo could not be saved. Try another file.", "Impossible d'enregistrer la photo. Essayez un autre fichier."),
"Treatment_Saved": ("Saved.", "Enregistré."),

# Counts
"Works_One": ("{0} work", "{0} œuvre"), "Works_Many": ("{0} works", "{0} œuvres"),
"Critical_One": ("{0} critical", "{0} critique"), "Critical_Many": ("{0} critical", "{0} critiques"),
"Overdue_One": ("{0} overdue", "{0} en retard"), "Overdue_Many": ("{0} overdue", "{0} en retard"),
"OpenTreatments_One": ("{0} open treatment", "{0} traitement en cours"), "OpenTreatments_Many": ("{0} open treatments", "{0} traitements en cours"),
"Photos_One": ("{0} photo", "{0} photo"), "Photos_Many": ("{0} photos", "{0} photos"),
"Open_One": ("{0} open", "{0} en cours"), "Open_Many": ("{0} open", "{0} en cours"),
"InProgress_One": ("{0} in progress", "{0} en traitement"), "InProgress_Many": ("{0} in progress", "{0} en traitement"),
"ThisWeek_One": ("{0} scheduled this week", "{0} prévu cette semaine"), "ThisWeek_Many": ("{0} scheduled this week", "{0} prévus cette semaine"),

# Problems
"Problem_None": ("", ""),
"Problem_SurveyorRequired": ("Enter the surveyor's name before submitting.", "Indiquez le nom de la personne qui inspecte avant de remettre."),
"Problem_FindingZoneRequired": ("Say where on the object each finding is.", "Indiquez où se trouve chaque constat sur l'œuvre."),
"Problem_SurveyNotFound": ("This survey no longer exists.", "Cette inspection n'existe plus."),
"Problem_SurveyAlreadySubmitted": ("This survey was already submitted.", "Cette inspection a déjà été remise."),
"Problem_ArtworkNotFound": ("This artwork is no longer in the collection.", "Cette œuvre n'est plus dans la collection."),
"Problem_TreatmentNotFound": ("This treatment is no longer in the queue.", "Ce traitement n'est plus dans la file."),
"Problem_ScheduleDateRequired": ("Pick a date to schedule the work.", "Choisissez une date pour planifier le travail."),
"Problem_CompletionNoteRequired": ("Add a note saying what was done before marking it done.", "Ajoutez une note sur ce qui a été fait avant de le marquer comme terminé."),
"Problem_TreatmentAlreadyDone": ("This treatment is done and can no longer change.", "Ce traitement est terminé et ne peut plus être modifié."),
"Problem_NoEarlierStatus": ("This treatment is already at its first step.", "Ce traitement est déjà à la première étape."),
"Problem_ImportNotPatinaFile": ("That file is not a Patina collection export.", "Ce fichier n'est pas une exportation de collection Patina."),
"Problem_ImportNewerVersion": ("That file was exported by a newer version of Patina. Update the app first.", "Ce fichier a été exporté par une version plus récente de Patina. Mettez d'abord l'application à jour."),

# Next action per status
"NextAction_Proposed": ("Schedule", "Planifier"),
"NextAction_Scheduled": ("Start work", "Commencer le travail"),
"NextAction_InProgress": ("Mark done", "Marquer comme terminé"),
"NextAction_Done": ("Done", "Terminé"),
}

ENUMS = {
"ArtMaterial": {"Bronze": ("Bronze", "bronze"), "Steel": ("Steel", "acier"), "Stone": ("Stone", "pierre"), "Concrete": ("Concrete", "béton"), "Mosaic": ("Mosaic", "mosaïque"), "Mural": ("Mural", "murale"), "Wood": ("Wood", "bois")},
"ConditionGrade": {"Unsurveyed": ("Not surveyed", "Non inspectée"), "Good": ("Good", "Bon"), "Fair": ("Fair", "Passable"), "Poor": ("Poor", "Mauvais"), "Critical": ("Critical", "Critique")},
"FindingType": {"Corrosion": ("Corrosion", "Corrosion"), "Graffiti": ("Graffiti", "Graffiti"), "Cracking": ("Cracking", "Fissuration"), "MaterialLoss": ("Material loss", "Perte de matière"), "BiologicalGrowth": ("Biological growth", "Croissance biologique"), "Flaking": ("Flaking paint", "Écaillage"), "Fading": ("Fading", "Décoloration"), "Efflorescence": ("Efflorescence", "Efflorescence"), "CoatingFailure": ("Coating failure", "Défaillance du revêtement"), "StructuralMovement": ("Structural movement", "Mouvement structural"), "Soiling": ("Soiling", "Encrassement"), "Vandalism": ("Vandalism damage", "Dommage par vandalisme")},
"Severity": {"Minor": ("Minor", "Mineur"), "Moderate": ("Moderate", "Modéré"), "Serious": ("Serious", "Sérieux"), "Urgent": ("Urgent", "Urgent")},
"Weather": {"Dry": ("Dry", "Sec"), "Overcast": ("Overcast", "Couvert"), "Wet": ("Wet", "Pluvieux"), "Freezing": ("Freezing", "Gel"), "Hot": ("Hot", "Chaud")},
"TreatmentStatus": {"Proposed": ("Proposed", "Proposé"), "Scheduled": ("Scheduled", "Planifié"), "InProgress": ("In progress", "En cours"), "Done": ("Done", "Terminé")},
"TreatmentPriority": {"Routine": ("Routine", "Courant"), "High": ("High priority", "Prioritaire"), "Urgent": ("Urgent", "Urgent")},
"CollectionFilter": {"All": ("All", "Toutes"), "NeedsAttention": ("Needs attention", "À surveiller"), "Overdue": ("Overdue", "En retard"), "Unsurveyed": ("Never surveyed", "Jamais inspectées")},
"QueueFilter": {"Open": ("Open", "En cours"), "Proposed": ("Proposed", "Proposés"), "Scheduled": ("Scheduled", "Planifiés"), "InProgress": ("In progress", "En traitement"), "Done": ("Done", "Terminés")},
}
VERBS = {"Corrosion": ("Treat corrosion", "Traiter la corrosion"), "Graffiti": ("Remove graffiti", "Retirer les graffitis"), "Cracking": ("Repair cracking", "Réparer les fissures"), "MaterialLoss": ("Consolidate losses", "Consolider les pertes"), "BiologicalGrowth": ("Remove biological growth", "Retirer la croissance biologique"), "Flaking": ("Consolidate flaking paint", "Consolider la peinture écaillée"), "Fading": ("Assess fading", "Évaluer la décoloration"), "Efflorescence": ("Reduce efflorescence", "Réduire l'efflorescence"), "CoatingFailure": ("Renew protective coating", "Renouveler le revêtement protecteur"), "StructuralMovement": ("Stabilize structure", "Stabiliser la structure"), "Soiling": ("Clean surface", "Nettoyer la surface"), "Vandalism": ("Repair vandalism damage", "Réparer les dommages de vandalisme")}

for e, vals in ENUMS.items():
    for k, v in vals.items():
        T[f"{e}_{k}"] = v
for k, v in VERBS.items():
    T[f"TreatmentVerb_{k}"] = v

# Coverage check: every key used in code/XAML has a string.
used = set()
for f in glob.glob(root + '/**/*.xaml', recursive=True):
    s = open(f, encoding='utf-8').read()
    for m in re.finditer(r'x:Uid="([^"]+)"', s):
        used.add(('uid', m.group(1)))
for f in glob.glob(root + '/**/*.cs', recursive=True):
    s = open(f, encoding='utf-8').read()
    for m in re.findall(r'"([A-Z][A-Za-z]+_[A-Za-z]+)"', s):
        used.add(('key', m))
uid_prefixes = {k.split('.')[0] for k in T}
missing = sorted(k for kind, k in used if k not in T and k not in uid_prefixes)
missing = [m for m in missing if not m.startswith(('Problem_', 'NextAction_'))]
if missing:
    print("MISSING:", missing)

HEAD = '''<?xml version="1.0" encoding="utf-8"?>
<root>
  <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>
  <resheader name="version"><value>2.0</value></resheader>
  <resheader name="reader"><value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
  <resheader name="writer"><value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
'''
for i, lang in enumerate(['en', 'fr']):
    out = [HEAD]
    for k in sorted(T):
        v = T[k][i]
        out.append(f'  <data name="{html.escape(k)}" xml:space="preserve"><value>{html.escape(v, quote=False)}</value></data>\n')
    out.append('</root>\n')
    open(f'{root}/Strings/{lang}/Resources.resw', 'w', encoding='utf-8').write(''.join(out))
print(len(T), "strings per language")
