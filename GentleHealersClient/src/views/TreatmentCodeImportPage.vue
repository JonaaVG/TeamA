<script setup>
import { computed, nextTick, ref } from 'vue'
import { useRouter } from 'vue-router'
import { Check, CircleAlert, LoaderCircle } from 'lucide-vue-next'
import nl from '@/features/treatmentCodes/nl.json'
import api from '@/services/api'
import {
  getTreatmentCodeImportError,
  readTreatmentCodeImportResult,
  treatmentCodeFileAccept,
  validateTreatmentCodeFile,
} from '@/services/treatmentCodeImport'

const text = nl.TreatmentCodeImportPage
const router = useRouter()
const fileInput = ref(null)
const feedback = ref(null)
const selectedFile = ref(null)
const isLoading = ref(false)
const errorMessage = ref('')
const successMessage = ref('')
const loginRequired = ref(false)

const canImport = computed(() => selectedFile.value && !validateTreatmentCodeFile(selectedFile.value) && !isLoading.value)

function selectFile(event) {
  selectedFile.value = event.target.files?.[0] || null
  successMessage.value = ''
  loginRequired.value = false
  errorMessage.value = selectedFile.value ? validateTreatmentCodeFile(selectedFile.value) : ''
}

function openFilePicker() {
  if (!isLoading.value) fileInput.value?.click()
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
  <div class="treatment-import-page">
    <main class="import-main">
      <h1 id="page-title" tabindex="-1">{{ text.title }}</h1>
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
          :accept="treatmentCodeFileAccept"
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
          <LoaderCircle v-if="isLoading" class="loading-icon" :size="18" aria-hidden="true" />
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
          <CircleAlert v-if="errorMessage" :size="20" aria-hidden="true" />
          <Check v-else :size="20" aria-hidden="true" />
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
  min-height: calc(100svh - 60px);
  background: #eaf4fd;
  color: #12366a;
  font-family: 'TreatmentCodeInter', Arial, sans-serif;
  font-size: 14px;
  line-height: 1.4;
  text-align: left;
}

.treatment-import-page *,
.treatment-import-page *::before,
.treatment-import-page *::after {
  box-sizing: border-box;
}

.import-main button {
  font: inherit;
  cursor: pointer;
}

.import-main {
  padding: 20px 16px;
}

.import-main h1 {
  margin: 0 0 6px;
  color: #12366a;
  font-size: 17px;
  font-weight: 700;
  line-height: 1.2;
}

.import-introduction {
  max-width: 820px;
  margin: 0;
}

.import-card {
  margin-top: 16px;
  border: 1px solid #788ba6;
  border-radius: 8px;
  padding: 20px;
  background: white;
}

.file-label {
  display: block;
  margin-bottom: 6px;
  font-weight: 700;
}

.file-selection {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  align-items: stretch;
  gap: 12px;
}

.selected-file {
  display: flex;
  align-items: center;
  min-height: 40px;
  min-width: 0;
  border: 1px solid #788ba6;
  border-radius: 8px;
  padding: 8px 12px;
  color: #788ba6;
  overflow-wrap: anywhere;
}

.selected-file.has-file {
  color: #12366a;
}

.choose-file-button {
  min-height: 40px;
  border: 2px solid #12366a;
  border-radius: 8px;
  padding: 8px 16px;
  background: white;
  color: #12366a;
  font-weight: 700 !important;
}

.file-help {
  margin: 16px 0;
}

.import-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  min-height: 40px;
  border: 2px solid #12366a;
  border-radius: 8px;
  padding: 8px 16px;
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
  margin-top: 20px;
  border: 2px solid #12366a;
  border-radius: 8px;
  padding: 12px 16px;
  background: white;
}

.feedback-heading {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 6px;
}

.feedback-heading h2 {
  margin: 0;
  color: inherit;
  font-size: 16px;
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

.treatment-import-page button:focus-visible {
  outline: 3px solid #0876d1;
  outline-offset: 4px;
}

.loading-icon {
  animation: import-loading 1s linear infinite;
}

@keyframes import-loading {
  to { transform: rotate(360deg); }
}

@media (max-width: 600px) {
  .import-main h1 { overflow-wrap: anywhere; }
  .import-card { padding: 16px; }
  .file-selection { grid-template-columns: minmax(0, 1fr); }
  .choose-file-button { justify-self: start; }
  .import-button { width: 100%; }
}

@media (prefers-reduced-motion: reduce) {
  .loading-icon { animation: none; }
}
</style>
