// Client-side navigation only; the API independently validates the signed JWT and role.
export function getTreatmentCodeClaims(token, now = Date.now()) {
  try {
    if (typeof token !== 'string' || token.split('.').length !== 3) return null
    const payload = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')
    const padded = payload.padEnd(Math.ceil(payload.length / 4) * 4, '=')
    const bytes = Uint8Array.from(atob(padded), (character) => character.charCodeAt(0))
    const claims = JSON.parse(new TextDecoder().decode(bytes))
    if (!claims || typeof claims.exp !== 'number' || claims.exp * 1000 <= now) return null
    if (typeof claims.nbf === 'number' && claims.nbf * 1000 > now) return null
    return claims
  } catch {
    return null
  }
}

export function canImportTreatmentCodes(token, now = Date.now()) {
  const claims = getTreatmentCodeClaims(token, now)
  const role = claims?.role ?? claims?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
  return Array.isArray(role) ? role.includes('SystemAdministrator') : role === 'SystemAdministrator'
}
