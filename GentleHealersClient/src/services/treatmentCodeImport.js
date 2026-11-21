import nl from '../features/treatmentCodes/nl.json' with { type: 'json' }

const text = nl.TreatmentCodeImport
const allowedExtensions = ['.xlsx', '.xls', '.csv']
export const treatmentCodeFileAccept = allowedExtensions.join(',')

export function validateTreatmentCodeFile(file) {
  if (!file) return text.fileRequired
  const fileName = (file.name ?? '').toLowerCase()
  if (!allowedExtensions.some(extension => fileName.endsWith(extension))) {
    return text.unsupportedFileType
  }
  if (file.size === 0) return text.emptyFile
  return ''
}

export function readTreatmentCodeImportResult(data) {
  if (!Number.isInteger(data?.importedCount) || data.importedCount <= 0) {
    throw new Error(text.unconfirmedImport)
  }
  return {
    importedCount: data.importedCount,
    message: typeof data.message === 'string' && data.message.trim()
      ? data.message
      : text.successfulImport,
  }
}

export function getTreatmentCodeImportError(error) {
  const status = error.response?.status
  if (status === 401) return text.expiredSession
  if (status === 403) return text.forbiddenImport
  if (status === 413) return text.fileTooLarge
  if (status === 415) return text.unsupportedFileType

  if (status === 400 || status === 422) {
    const data = error.response.data
    const message = data?.message || data?.detail || data?.title
    if (typeof message === 'string' && message.trim()) return message
    return text.unreadableFile
  }
  if (error.request && !error.response) {
    return text.connectionError
  }
  return text.importFailed
}
