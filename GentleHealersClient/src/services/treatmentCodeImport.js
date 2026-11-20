const excelFilePattern = /\.(xlsx|xls)$/i

export function validateTreatmentCodeFile(file) {
  if (!file) return 'Kies eerst een Excel-bestand om te importeren.'
  if (!excelFilePattern.test(file.name ?? '')) {
    return 'Kies een Excel-bestand (.xlsx of .xls). Andere bestandstypen worden niet geïmporteerd.'
  }
  if (file.size === 0) return 'Het bestand is leeg. Kies het aangeleverde Excel-bestand.'
  return ''
}

export function readTreatmentCodeImportResult(data) {
  if (!Number.isInteger(data?.importedCount) || data.importedCount <= 0) {
    throw new Error('De server kon de import niet bevestigen. Probeer het opnieuw.')
  }
  return {
    importedCount: data.importedCount,
    message: typeof data.message === 'string' && data.message.trim()
      ? data.message
      : 'Bestand is succesvol geïmporteerd.',
  }
}

export function getTreatmentCodeImportError(error) {
  const status = error.response?.status
  if (status === 401) return 'Je sessie is verlopen. Log opnieuw in om een bestand te importeren.'
  if (status === 403) return 'Je hebt geen toestemming om behandelcodes te importeren. Dit kan alleen als systeembeheerder.'
  if (status === 413) return 'Het bestand is te groot. Kies een kleiner Excel-bestand.'
  if (status === 415) return 'Kies een Excel-bestand (.xlsx of .xls). Andere bestandstypen worden niet geïmporteerd.'

  if (status === 400 || status === 422) {
    const data = error.response.data
    const message = data?.message || data?.detail || data?.title
    if (typeof message === 'string' && message.trim()) return message
    return 'Het bestand kon niet worden ingelezen. Controleer of je het aangeleverde Excel-bestand hebt gekozen.'
  }
  if (error.request && !error.response) {
    return 'Er is geen verbinding met de server. Controleer je verbinding en probeer het opnieuw.'
  }
  return 'Importeren is niet gelukt. Probeer het opnieuw.'
}
