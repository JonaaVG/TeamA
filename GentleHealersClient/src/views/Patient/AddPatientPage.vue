<template>
    <div class="min-h-screen bg-slate-50 flex items-center justify-center p-6">
        <div class="w-full max-w-3xl bg-white rounded-2xl shadow-sm border border-slate-200 p-8">

            <!-- Titel en Subtitel -->
            <div class="mb-8">
                <h1 class="text-2xl font-bold text-slate-900 " id="page-title"  tabindex="-1">Patiëntaccount aanmaken</h1>
                <p class="text-sm text-slate-500 mt-1">Vul de basisgegevens van de patiënt in.</p>
            </div>

            <!-- Fout- of succesmeldingen -->
            <div v-if="errorMessage" class="mb-6 p-4 bg-red-50 border border-red-200 text-red-700 rounded-xl text-sm" id="patient-error">
                {{ errorMessage }}
            </div>
            <div v-if="successMessage"
                class="mb-6 p-4 bg-green-50 border border-green-200 text-green-700 rounded-xl text-sm" id="patient-success">
                {{ successMessage }}
            </div>

            <!-- Formulier -->
            <form @submit.prevent="handleSubmit" class="space-y-6">

                <!-- Grid met formulier velden -->
                <div class="grid grid-cols-1 md:grid-cols-2 gap-6">

                    <!-- Firstname -->
                    <div class="space-y-2">
                        <label class="block text-sm font-medium text-slate-700">Voornaam</label>
                        <input type="text" v-model="form.firstname" placeholder="Voornaam patiënt" aria-label="Invoerveld voornaam patiënt" id="patient-firstname"required
                            class="w-full px-4 py-3 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-blue-900 focus:border-transparent text-slate-800 placeholder-slate-400" />
                    </div>
                    <!-- Lastname -->
                    <div class="space-y-2">
                        <label class="block text-sm font-medium text-slate-700">Achternaam</label>
                        <input v-model="form.lastname" placeholder="Achternaam patiënt" aria-label="Invoerveld achternaam patiënt" id="patient-lastname"required
                            class="w-full px-4 py-3 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-blue-900 focus:border-transparent text-slate-800 placeholder-slate-400" />
                    </div>
                    <!-- Email -->
                    <div class="space-y-2">
                        <label class="block text-sm font-medium text-slate-700">E-mailadres</label>
                        <input type="email" v-model="form.email" placeholder="naam@voorbeeld.nl" aria-label="Invoerveld e-mailadres patiënt" id="patient-email" required
                            class="w-full px-4 py-3 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-blue-900 focus:border-transparent text-slate-800 placeholder-slate-400" />
                    </div>

                    <!-- BSN -->
                    <div class="space-y-2">
                        <label class="block text-sm font-medium text-slate-700">BSN</label>
                        <input type="text" v-model="form.bsn" placeholder="BSN" aria-label="Invoerveld BSN patiënt" id="patient-bsn" required
                            class="w-full px-4 py-3 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-blue-900 focus:border-transparent text-slate-800 placeholder-slate-400" />
                    </div>

                    <!-- Phone number -->
                    <div class="space-y-2">
                        <label class="block text-sm font-medium text-slate-700">Telefoonnummer</label>
                        <input type="tel" v-model="form.phone" placeholder="Telefoonnummer" aria-label="Invoerveld telefoonnummer patiënt" id="patient-phone" required
                            class="w-full px-4 py-3 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-blue-900 focus:border-transparent text-slate-800 placeholder-slate-400" />
                    </div>

                    <!-- Date of birth -->
                    <div class="space-y-2 md:col-span-2">
                        <label class="block text-sm font-medium text-slate-700">Geboortedatum</label>
                        <input type="date" v-model="form.dateOfBirth" :max="today" :min="minDate" aria-label="datumveld geboortedatum patiënt " id="patient-date-of-birth" required
                            class="w-full px-4 py-3 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-blue-900 focus:border-transparent text-slate-800" />
                    </div>

                </div>

                <!-- Bottom buttons -->
                <div class="flex items-center justify-end gap-4 pt-6 border-t border-slate-100">
                    <button type="button" @click="handleCancel"
                        class="px-6 py-3 rounded-xl border border-slate-300 text-slate-700 font-medium hover:bg-slate-50 transition">
                        Annuleren
                    </button>

                    <button id="patient-submit" type="submit" :disabled="isLoading"
                        class="px-6 py-3 rounded-xl bg-blue-950 text-white font-medium hover:bg-blue-900 transition disabled:opacity-50">
                        {{ isLoading ? 'Bezig...' : 'Account aanmaken' }}
                    </button>
                </div>

            </form>
        </div>
    </div>
</template>

<script setup>
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import api from '@/services/api';

const router = useRouter()
const isLoading = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const today = new Date().toISOString().split('T')[0]
const minDate = new Date(new Date().setFullYear(new Date().getFullYear() - 150))
    .toISOString().split('T')[0]
    
const emptyForm = () => ({
    firstname: '',
    lastname: '',
    email: '',
    bsn: '',
    phone: '',
    dateOfBirth: ''
})

const form = reactive(emptyForm())

const resetForm = () => {
    Object.assign(form, emptyForm())
}

const handleCancel = () => {
    router.push('/dashboard')
}
// Form submission handler
const handleSubmit = async () => {
    errorMessage.value = ''
    successMessage.value = ''
    isLoading.value = true
    
    try {
        if (!isValidPhone(form.phone)) {
            errorMessage.value = 'Vul een geldig telefoonnummer in.'
            return
        }
        // Send the form data to the backend API
        await api.post('/patient/register', {
            user: {
                firstName: form.firstname,
                lastName: form.lastname,
                email: form.email,
                roleName: 'Patient'
            },
            bsn: form.bsn,
            dateOfBirth: form.dateOfBirth,
            phone: form.phone
        })

        successMessage.value =
            'Patiëntaccount succesvol aangemaakt!'
        resetForm()
    } catch (error) {
        errorMessage.value =
            error.response?.data?.message ||
            'Er is iets misgegaan bij het aanmaken van de patiënt.'
    } finally {
        isLoading.value = false
    }
}
// validates if the phone number is in a valid format (basic check)
const isValidPhone = (value) => {
    const trimmed = value.trim()
    if (!/^\+?[0-9\s\-().]+$/.test(trimmed)) return false
    const digits = trimmed.replace(/\D/g, '').length
    return digits >= 8 && digits <= 15
}
</script>
