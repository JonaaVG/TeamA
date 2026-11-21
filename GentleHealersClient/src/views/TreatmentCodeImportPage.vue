<script setup>
import { computed, nextTick, ref } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import { Check, ChevronDown, CircleAlert, LoaderCircle, LogOut, Menu } from 'lucide-vue-next'
import nl from '@/features/treatmentCodes/nl.json'
import api from '@/services/api'
import {
  getTreatmentCodeImportError,
  readTreatmentCodeImportResult,
  validateTreatmentCodeFile,
} from '@/services/treatmentCodeImport'

const text = nl.TreatmentCodeImportPage
const router = useRouter()
const fileInput = ref(null)
const feedback = ref(null)
const pageHeading = ref(null)
const navigation = ref(null)
const selectedFile = ref(null)
const isLoading = ref(false)
const errorMessage = ref('')
const successMessage = ref('')
const loginRequired = ref(false)
const menuOpen = ref(false)
const accountOpen = ref(false)

const canImport = computed(() => selectedFile.value && !validateTreatmentCodeFile(selectedFile.value) && !isLoading.value)

function readCurrentUser() {
  try {
    const encoded = localStorage.getItem('token')?.split('.')[1]
    if (!encoded) return {}
    const base64 = encoded.replace(/-/g, '+').replace(/_/g, '/')
    const bytes = Uint8Array.from(atob(base64), character => character.charCodeAt(0))
    return JSON.parse(new TextDecoder().decode(bytes))
  } catch {
    return {}
  }
}

const currentUser = readCurrentUser()
const accountName = currentUser.name || currentUser.unique_name || currentUser.email
  || currentUser['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress']
  || text.administrator
const accountId = currentUser.sub || currentUser.nameid
  || currentUser['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']
const accountNumber = accountId ? text.accountNumber.replace('{id}', () => String(accountId)) : ''

function selectFile(event) {
  selectedFile.value = event.target.files?.[0] || null
  successMessage.value = ''
  loginRequired.value = false
  errorMessage.value = selectedFile.value ? validateTreatmentCodeFile(selectedFile.value) : ''
}

function openFilePicker() {
  if (!isLoading.value) fileInput.value?.click()
}

async function toggleMenu() {
  menuOpen.value = !menuOpen.value
  accountOpen.value = false
  if (menuOpen.value) {
    await nextTick()
    navigation.value?.querySelector('a')?.focus()
  }
}

function closeMenus() {
  menuOpen.value = false
  accountOpen.value = false
}

function logout() {
  localStorage.removeItem('token')
  localStorage.removeItem('userRole')
  router.push({ name: 'login' })
}

async function importFile() {
  if (isLoading.value) return
  successMessage.value = ''
  errorMessage.value = validateTreatmentCodeFile(selectedFile.value)
  if (errorMessage.value) return

  isLoading.value = true
  loginRequired.value = false

  try {
    const form = new FormData()
    form.append('file', selectedFile.value)
    const response = await api.post('/admin/treatment-codes/import', form)
    const result = readTreatmentCodeImportResult(response.data)
    successMessage.value = result.message
  } catch (error) {
    loginRequired.value = error.response?.status === 401
    errorMessage.value = getTreatmentCodeImportError(error)
  } finally {
    isLoading.value = false
    await nextTick()
    feedback.value?.focus()
  }
}
</script>

<template>
  <div class="treatment-import-page" @keydown.esc="closeMenus">
    <a class="import-skip-link" href="#page-title" @click.prevent="pageHeading?.focus()">
      {{ text.skipToContent }}
    </a>

    <header class="import-header">
      <div class="menu-container">
        <button
          class="menu-toggle"
          type="button"
          :aria-label="menuOpen ? text.closeMenu : text.openMenu"
          :aria-expanded="menuOpen"
          aria-controls="import-navigation"
          @click="toggleMenu"
        >
          <Menu :size="50" :stroke-width="2.3" aria-hidden="true" />
          <span>{{ text.menu }}</span>
        </button>
        <nav v-if="menuOpen" id="import-navigation" ref="navigation" class="navigation-panel" :aria-label="text.mainNavigation">
          <RouterLink to="/" @click="closeMenus">{{ text.dashboard }}</RouterLink>
          <RouterLink to="/admin/treatment-codes/import" @click="closeMenus">{{ text.title }}</RouterLink>
        </nav>
      </div>

      <RouterLink to="/" class="hospital-brand" :aria-label="text.brandAriaLabel">
        <span>{{ text.brandFirst }}</span> <span class="brand-accent">{{ text.brandSecond }}</span>
      </RouterLink>

      <div class="account-container">
        <button
          type="button"
          class="account-toggle"
          :aria-expanded="accountOpen"
          aria-controls="import-account"
          @click="accountOpen = !accountOpen; menuOpen = false"
        >
          <span class="account-summary">
            <span class="account-name" :title="accountName">{{ accountName }}</span>
            <span v-if="accountNumber">{{ accountNumber }}</span>
            <span v-if="accountName !== text.administrator">{{ text.administrator }}</span>
          </span>
          <ChevronDown :size="26" aria-hidden="true" />
        </button>
        <div v-if="accountOpen" id="import-account" class="account-panel">
          <p>{{ accountName }}</p>
          <p v-if="accountNumber">{{ accountNumber }}</p>
          <p>{{ text.administratorRole }}</p>
        </div>
      </div>

      <button type="button" class="logout-button" :aria-label="text.logout" @click="logout">
        <LogOut :size="56" :stroke-width="1.9" aria-hidden="true" />
      </button>
    </header>

    <main class="import-main">
      <h1 id="page-title" ref="pageHeading" tabindex="-1">{{ text.title }}</h1>
      <p class="import-introduction">
        {{ text.introduction }}
      </p>

      <form class="import-card" :aria-busy="isLoading" @submit.prevent="importFile" novalidate>
        <label class="file-label" for="treatment-code-file">{{ text.fileLabel }}</label>
        <input
          id="treatment-code-file"
          ref="fileInput"
          type="file"
          name="file"
          accept=".xlsx,.xls"
          hidden
          :disabled="isLoading"
          @change="selectFile"
        />

        <div class="file-selection">
          <span id="selected-treatment-code-file" class="selected-file" :class="{ 'has-file': selectedFile }" aria-live="polite">
            {{ selectedFile?.name || text.filePlaceholder }}
          </span>
          <button
            class="choose-file-button"
            type="button"
            :disabled="isLoading"
            aria-controls="treatment-code-file"
            :aria-describedby="`file-type-help selected-treatment-code-file${errorMessage ? ' import-feedback' : ''}`"
            @click="openFilePicker"
          >
            {{ text.chooseFile }}
          </button>
        </div>

        <p id="file-type-help" class="file-help">{{ text.fileTypeHelp }}</p>

        <button class="import-button" type="submit" :disabled="!canImport">
          <LoaderCircle v-if="isLoading" class="loading-icon" :size="26" aria-hidden="true" />
          {{ isLoading ? text.importInProgress : text.submitButton }}
        </button>
      </form>

      <section
        v-if="errorMessage || successMessage"
        id="import-feedback"
        ref="feedback"
        class="import-feedback"
        :class="{ 'error-feedback': errorMessage }"
        :role="errorMessage ? 'alert' : 'status'"
        :aria-labelledby="errorMessage ? 'import-error-heading' : 'import-success-heading'"
        tabindex="-1"
      >
        <div class="feedback-heading">
          <CircleAlert v-if="errorMessage" :size="30" aria-hidden="true" />
          <Check v-else :size="30" aria-hidden="true" />
          <h2 v-if="errorMessage" id="import-error-heading">{{ text.importFailed }}</h2>
          <h2 v-else id="import-success-heading">{{ text.importSucceeded }}</h2>
        </div>
        <p>{{ errorMessage || successMessage }}</p>
        <button v-if="loginRequired" class="login-again-button" type="button" @click="logout">{{ text.loginAgain }}</button>
      </section>
    </main>
  </div>
</template>

<style scoped>
@font-face {
  font-family: 'TreatmentCodeInter';
  src: url('../assets/treatment-codes/InterVariable.woff2') format('woff2');
  font-weight: 100 900;
  font-style: normal;
  font-display: swap;
}

.treatment-import-page {
  min-height: 100svh;
  background: #eaf4fd;
  color: #12366a;
  font-family: 'TreatmentCodeInter', Arial, sans-serif;
  font-size: 24px;
  line-height: 1.2;
  text-align: left;
}

.treatment-import-page *,
.treatment-import-page *::before,
.treatment-import-page *::after {
  box-sizing: border-box;
}

.import-skip-link {
  position: fixed;
  z-index: 30;
  top: 0;
  left: 16px;
  transform: translateY(-120%);
  border: 2px solid #12366a;
  padding: 14px;
  background: white;
  color: #12366a;
  font-size: 18px;
}

.import-skip-link:focus {
  transform: translateY(0);
}

.import-header {
  display: grid;
  grid-template-columns: 330px minmax(0, 1fr) 416px 136px;
  min-height: 146px;
  border-radius: 0 0 18px 18px;
  background: white;
  box-shadow: 0 3px 3px #00000030;
}

.import-header button,
.import-main button {
  font: inherit;
  cursor: pointer;
}

.import-header button {
  color: #12366a;
}

.menu-container,
.account-container {
  position: relative;
}

.menu-toggle {
  display: flex;
  align-items: center;
  gap: 30px;
  width: 100%;
  min-height: 146px;
  border: 0;
  padding: 30px 38px;
  background: transparent;
  font-size: 40px !important;
  font-weight: 700 !important;
}

.hospital-brand {
  display: flex;
  align-items: center;
  justify-content: flex-start;
  flex-wrap: wrap;
  column-gap: 10px;
  padding: 20px 38px;
  color: #12366a;
  font-size: 40px;
  font-weight: 700;
  letter-spacing: -0.5px;
  text-decoration: none;
}

.brand-accent {
  color: #0876d1;
}

.account-toggle {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 14px;
  width: 100%;
  height: 100%;
  border: 1px solid #788ba6;
  padding: 25px 20px 25px 48px;
  background: transparent;
  text-align: left;
}

.account-summary {
  display: flex;
  flex-direction: column;
  min-width: 0;
  font-size: 20px;
  line-height: 1.2;
}

.account-name {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.account-toggle svg,
.menu-toggle svg {
  flex-shrink: 0;
}

.logout-button {
  display: flex;
  align-items: center;
  justify-content: center;
  border: 0;
  padding: 20px;
  background: transparent;
}

.navigation-panel,
.account-panel {
  position: absolute;
  z-index: 10;
  top: calc(100% + 8px);
  left: 12px;
  width: max-content;
  max-width: min(430px, calc(100vw - 24px));
  border: 1px solid #788ba6;
  border-radius: 12px;
  padding: 12px;
  background: white;
  box-shadow: 0 8px 20px #12366a20;
  font-size: 18px;
}

.navigation-panel a {
  display: block;
  border-radius: 6px;
  padding: 14px;
  color: #12366a;
  text-decoration: none;
}

.navigation-panel a:hover,
.navigation-panel a.router-link-exact-active {
  background: #eaf4fd;
}

.account-panel {
  right: 12px;
  left: auto;
  padding: 20px;
  overflow-wrap: anywhere;
}

.account-panel p {
  margin: 0 0 8px;
}

.import-main {
  padding: 54px 38px 34px;
}

.import-main h1 {
  margin: 0 0 18px;
  color: #12366a;
  font-size: 40px;
  font-weight: 700;
  line-height: 1.2;
}

.import-introduction {
  max-width: 820px;
  margin: 0;
  font-size: 24px;
  line-height: 1;
}

.import-card {
  margin-top: 44px;
  border: 1px solid #788ba6;
  border-radius: 18px;
  padding: 59px 48px 38px;
  background: white;
}

.file-label {
  display: block;
  margin-bottom: 18px;
  font-size: 26px;
  font-weight: 700;
}

.file-selection {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 246px;
  align-items: stretch;
  gap: 90px;
}

.selected-file {
  display: flex;
  align-items: center;
  min-height: 88px;
  min-width: 0;
  border: 1px solid #788ba6;
  border-radius: 18px;
  padding: 20px 30px;
  color: #788ba6;
  overflow-wrap: anywhere;
}

.selected-file.has-file {
  color: #12366a;
}

.choose-file-button {
  min-height: 88px;
  border: 2px solid #12366a;
  border-radius: 18px;
  padding: 20px;
  background: white;
  color: #12366a;
  font-weight: 700 !important;
}

.file-help {
  margin: 52px 0;
  font-size: 24px;
}

.import-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 12px;
  min-width: 216px;
  min-height: 89px;
  border: 2px solid #12366a;
  border-radius: 18px;
  padding: 20px 30px;
  background: #12366a;
  color: white;
  font-weight: 700 !important;
}

.import-button:hover:not(:disabled) {
  background: #092750;
}

.choose-file-button:hover:not(:disabled) {
  background: #eaf4fd;
}

.import-main button:disabled {
  cursor: not-allowed;
  opacity: 0.6;
}

.import-feedback {
  width: 850px;
  max-width: 100%;
  margin-top: 55px;
  border: 2px solid #12366a;
  border-radius: 18px;
  padding: 12px 28px 25px;
  background: white;
}

.feedback-heading {
  display: flex;
  align-items: center;
  gap: 24px;
  margin-bottom: 12px;
}

.feedback-heading h2 {
  margin: 0;
  color: inherit;
  font-size: 26px;
  font-weight: 700;
}

.feedback-heading svg {
  flex-shrink: 0;
}

.import-feedback p {
  margin: 0;
  overflow-wrap: anywhere;
}

.error-feedback {
  border-color: #922929;
  color: #7b2020;
}

.login-again-button {
  margin-top: 20px;
  border: 1px solid currentColor;
  border-radius: 8px;
  padding: 12px 18px;
  background: white;
  color: #7b2020;
  text-decoration: underline;
}

.treatment-import-page button:focus-visible,
.treatment-import-page a:focus-visible {
  outline: 3px solid #0876d1;
  outline-offset: 4px;
}

.loading-icon {
  animation: import-loading 1s linear infinite;
}

@keyframes import-loading {
  to { transform: rotate(360deg); }
}

@media (max-width: 1250px) {
  .import-header { grid-template-columns: 220px minmax(0, 1fr) 290px 96px; }
  .menu-toggle { gap: 18px; padding-inline: 25px; font-size: 30px !important; }
  .menu-toggle svg { width: 38px; }
  .hospital-brand { padding-inline: 24px; font-size: 30px; }
  .account-toggle { padding-inline: 22px; }
  .account-summary { font-size: 18px; }
  .file-selection { gap: 30px; }
}

@media (max-width: 900px) {
  .import-header { grid-template-columns: 160px minmax(0, 1fr) 76px; min-height: 100px; }
  .menu-toggle { min-height: 100px; gap: 12px; padding-inline: 20px; font-size: 24px !important; }
  .menu-toggle svg { width: 34px; }
  .hospital-brand { justify-content: center; padding: 18px 12px; font-size: 28px; column-gap: 7px; }
  .account-container { display: none; }
  .logout-button { padding: 18px; }
  .logout-button svg { width: 40px; }
  .import-main { padding: 38px 24px; }
  .import-main h1 { font-size: 34px; }
  .import-introduction { font-size: 21px; line-height: 1.3; }
  .import-card { padding: 32px; margin-top: 32px; }
  .file-selection { grid-template-columns: minmax(0, 1fr) 210px; gap: 20px; }
  .file-label, .feedback-heading h2 { font-size: 24px; }
  .selected-file, .choose-file-button { min-height: 72px; font-size: 20px; }
  .selected-file { padding: 18px; }
  .file-help { margin: 30px 0; font-size: 20px; }
  .import-button { min-height: 72px; font-size: 22px !important; }
  .import-feedback { margin-top: 32px; font-size: 20px; }
}

@media (max-width: 600px) {
  .treatment-import-page { font-size: 18px; }
  .import-header { grid-template-columns: 60px minmax(0, 1fr) 60px; min-height: 78px; border-radius: 0 0 12px 12px; }
  .menu-toggle { justify-content: center; min-height: 78px; padding: 14px; }
  .menu-toggle span { display: none; }
  .menu-toggle svg { width: 32px; height: 32px; }
  .hospital-brand { padding: 12px 4px; font-size: 21px; column-gap: 5px; letter-spacing: -0.4px; }
  .logout-button { padding: 15px; }
  .logout-button svg { width: 32px; height: 32px; }
  .import-main { padding: 30px 16px; }
  .import-main h1 { font-size: 28px; overflow-wrap: anywhere; }
  .import-introduction { font-size: 18px; line-height: 1.4; }
  .import-card { margin-top: 26px; padding: 24px 20px; border-radius: 14px; }
  .file-label { font-size: 21px; }
  .file-selection { grid-template-columns: minmax(0, 1fr); gap: 14px; }
  .selected-file, .choose-file-button { min-height: 60px; border-radius: 12px; font-size: 18px; }
  .choose-file-button { justify-self: start; padding: 14px 20px; }
  .file-help { margin: 24px 0; font-size: 17px; line-height: 1.4; }
  .import-button { min-width: 0; width: 100%; min-height: 62px; border-radius: 12px; font-size: 20px !important; padding: 14px; }
  .import-feedback { border-radius: 14px; padding: 18px 20px; font-size: 18px; }
  .feedback-heading { gap: 14px; }
  .feedback-heading h2 { font-size: 21px; }
}

@media (prefers-reduced-motion: reduce) {
  .loading-icon { animation: none; }
}
</style>
