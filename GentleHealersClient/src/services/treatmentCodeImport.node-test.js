import assert from 'node:assert/strict'
import test from 'node:test'
import {
  getTreatmentCodeImportError,
  readTreatmentCodeImportResult,
  validateTreatmentCodeFile,
} from './treatmentCodeImport.js'

test('TC-10.1.1-13 requires a selected, nonempty file', () => {
  assert.match(validateTreatmentCodeFile(null), /Kies eerst/)
  assert.match(validateTreatmentCodeFile({ name: 'behandelingen.xlsx', size: 0 }), /leeg/)
})

test('TC-10.1.1-14 confirms success only for the expected completed import response', () => {
  assert.deepEqual(readTreatmentCodeImportResult({ importedCount: 100, message: 'Import voltooid.' }), {
    importedCount: 100,
    message: 'Import voltooid.',
  })
  for (const result of [null, '<html>Vite</html>', {}, { importedCount: 0 }, { importedCount: '100' }]) {
    assert.throws(() => readTreatmentCodeImportResult(result), /niet bevestigen/)
  }
})

test('TC-10.1.1-15 gives a safe fallback when the API supplies no success message', () => {
  assert.equal(readTreatmentCodeImportResult({ importedCount: 1 }).message, 'Bestand is succesvol geïmporteerd.')
})

test('TC-10.1.1-16 explains expired authentication and missing permission', () => {
  assert.match(getTreatmentCodeImportError({ response: { status: 401 } }), /Log opnieuw in/)
  assert.match(getTreatmentCodeImportError({ response: { status: 403 } }), /geen toestemming/)
})

test('TC-10.1.2-05 displays the API validation message and accepted formats', () => {
  assert.equal(
    getTreatmentCodeImportError({ response: { status: 400, data: { message: 'De kolom Zorgcode ontbreekt.' } } }),
    'De kolom Zorgcode ontbreekt.',
  )
  assert.match(getTreatmentCodeImportError({ response: { status: 415 } }), /\(\.xlsx, \.xls of \.csv\)/)
})

test('TC-10.1.1-17 explains a failed connection', () => {
  assert.match(getTreatmentCodeImportError({ request: {} }), /verbinding/)
})

test('TC-10.1.1-18 does not expose internal server errors', () => {
  assert.equal(
    getTreatmentCodeImportError({ response: { status: 500, data: { message: 'SQL password or stack trace' } } }),
    'Importeren is niet gelukt. Probeer het opnieuw.',
  )
})
