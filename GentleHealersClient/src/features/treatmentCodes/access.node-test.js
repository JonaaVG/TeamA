import test from 'node:test'
import assert from 'node:assert/strict'
import { canImportTreatmentCodes, getTreatmentCodeClaims } from './access.js'

const now = Date.UTC(2026, 10, 19)
const token = (claims) => `header.${Buffer.from(JSON.stringify(claims)).toString('base64url')}.signature`
const valid = { role: 'SystemAdministrator', exp: now / 1000 + 60 }

test('TC-10.1.1-19 a system administrator with an unexpired token can open the import page', () => {
  assert.equal(canImportTreatmentCodes(token(valid), now), true)
})

test('TC-10.1.1-20 the existing Microsoft role claim is supported', () => {
  assert.equal(canImportTreatmentCodes(token({
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'SystemAdministrator',
    exp: valid.exp,
  }), now), true)
})

test('TC-10.1.1-21 other user roles cannot open the import page', () => {
  for (const role of ['Patient', 'GeneralPractitioner', 'Specialist', 'Administration']) {
    assert.equal(canImportTreatmentCodes(token({ ...valid, role }), now), false)
  }
})

test('TC-10.1.1-22 expired tokens and tokens without an expiry are rejected', () => {
  assert.equal(getTreatmentCodeClaims(token({ ...valid, exp: now / 1000 }), now), null)
  assert.equal(getTreatmentCodeClaims(token({ role: valid.role }), now), null)
})

test('TC-10.1.1-23 missing and malformed tokens are rejected without throwing', () => {
  for (const value of [null, undefined, '', 'not-a-token', 'a.%.c', 'a.bnVsbA.c']) {
    assert.equal(getTreatmentCodeClaims(value, now), null)
  }
})

test('TC-10.1.1-24 UTF-8 profile information is decoded correctly', () => {
  assert.equal(getTreatmentCodeClaims(token({ ...valid, name: 'Zoë' }), now).name, 'Zoë')
})
