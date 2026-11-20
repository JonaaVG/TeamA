<template>
  <div class="min-h-screen bg-slate-50 flex items-center justify-center p-6">
    <div class="w-full max-w-3xl bg-white rounded-2xl shadow-sm border border-slate-200 p-8">
      
      <!-- Titel en Subtitel -->
      <div class="mb-8">
        <h1 class="text-2xl font-bold text-slate-900">Patiëntaccount aanmaken</h1>
        <p class="text-sm text-slate-500 mt-1">Vul de basisgegevens van de patiënt in.</p>
      </div>

      <!-- Fout- of succesmeldingen -->
      <div v-if="errorMessage" class="mb-6 p-4 bg-red-50 border border-red-200 text-red-700 rounded-xl text-sm">
        {{ errorMessage }}
      </div>
      <div v-if="successMessage" class="mb-6 p-4 bg-green-50 border border-green-200 text-green-700 rounded-xl text-sm">
        {{ successMessage }}
      </div>

      <!-- Formulier -->
      <form @submit.prevent="handleSubmit" class="space-y-6">
        
        <!-- Grid met formulier velden (2 kolommen, zonder roepnaam) -->
        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
          
          <!-- Firstname -->
          <div class="space-y-2">
            <label class="block text-sm font-medium text-slate-700">Voornaam</label>
            <input 
              type="text" 
              v-model="form.firstname" 
              placeholder="Voornaam patiënt" 
              required
              class="w-full px-4 py-3 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-blue-900 focus:border-transparent text-slate-800 placeholder-slate-400"
            />
          </div>
          <!-- Lastname -->
          <div class="space-y-2">
            <label class="block text-sm font-medium text-slate-700">Achternaam</label>
            <input 
              v-model="form.lastname" 
              placeholder="Achternaam patiënt" 
              required
              class="w-full px-4 py-3 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-blue-900 focus:border-transparent text-slate-800 placeholder-slate-400"
            />
          </div>
          <!-- Email -->
          <div class="space-y-2">
            <label class="block text-sm font-medium text-slate-700">E-mailadres</label>
            <input 
              type="email" 
              v-model="form.email" 
              placeholder="naam@voorbeeld.nl" 
              required
              class="w-full px-4 py-3 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-blue-900 focus:border-transparent text-slate-800 placeholder-slate-400"
            />
          </div>

          <!-- BSN -->
          <div class="space-y-2">
            <label class="block text-sm font-medium text-slate-700">BSN</label>
            <input 
              type="text" 
              v-model="form.bsn" 
              placeholder="BSN" 
              required
              class="w-full px-4 py-3 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-blue-900 focus:border-transparent text-slate-800 placeholder-slate-400"
            />
          </div>

          <!-- Telefoonnummer -->
          <div class="space-y-2">
            <label class="block text-sm font-medium text-slate-700">Telefoonnummer</label>
            <input 
              type="tel" 
              v-model="form.phone" 
              placeholder="Telefoonnummer" 
              required
              class="w-full px-4 py-3 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-blue-900 focus:border-transparent text-slate-800 placeholder-slate-400"
            />
          </div>

          <!-- Geboortedatum -->
          <div class="space-y-2 md:col-span-2">
            <label class="block text-sm font-medium text-slate-700">Geboortedatum</label>
            <input 
              type="date" 
              v-model="form.dateOfBirth" 
              required
              class="w-full px-4 py-3 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-blue-900 focus:border-transparent text-slate-800"
            />
          </div>

        </div>

        <!-- Knoppen onderaan -->
        <div class="flex items-center justify-end gap-4 pt-6 border-t border-slate-100">
          <button 
            type="button" 
            @click="handleCancel"
            class="px-6 py-3 rounded-xl border border-slate-300 text-slate-700 font-medium hover:bg-slate-50 transition"
          >
            Annuleren
          </button>
          
          <button 
            type="submit" 
            :disabled="isLoading"
            class="px-6 py-3 rounded-xl bg-blue-950 text-white font-medium hover:bg-blue-900 transition disabled:opacity-50"
          >
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

const form = reactive({
  firstname: '',
  lastname: '',
  email: '',
  bsn: '',
  phone: '',
  dateOfBirth: ''
})

const handleCancel = () => {
  router.push('/dashboard')
}

const handleSubmit = async () => {
    errorMessage.value = ''
    successMessage.value = ''
    isLoading.value = true

    try {
        // Step 1: Create the user account first
        const userResponse = await api.post('/users/create', {
            firstName: form.firstname,
            lastName: form.lastname,
            email: form.email,
            roleName: 'Patient'
        })

        // Get the userId from the response
        const newUserId = userResponse.data.userId;

        // Step 2: Create the patient record using the userId
        await api.post(`/patient`, {
            userId: newUserId,
            bsn: form.bsn,
            phone: form.phone,
            dateOfBirth: form.dateOfBirth
        })

        successMessage.value =
            'Patiëntaccount succesvol aangemaakt!'

        setTimeout(() => {
            router.push('/dashboard')
        }, 1500)

    } catch (error) {
        errorMessage.value =
            error.response?.data?.message ||
            'Er is iets misgegaan bij het aanmaken van de patiënt.'
    } finally {
        isLoading.value = false
    }
}
</script>
