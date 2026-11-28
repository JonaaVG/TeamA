<template>
  <div class="space-y-8">
    <!-- Welcome Message -->
    <BaseCard aria-labelledby="page-title">
      <h1 id="page-title" tabindex="-1" class="text-2xl md:text-3xl font-bold text-gray-900 flex items-center gap-2">
        Testing Dashboard
    </h1>
    </BaseCard>

    <!-- Dashboard Grid -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">



    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '@/services/api'
import DataList from '@/components/base/DataList.vue'
// Reactive state for user profile and dashboard widgets

const isLoading = ref(true)
const errorMessage = ref('')
// Count completed exercises

// Fetch user profile and dashboard data on component mount using the centralized api service
// onMounted(async () => {
//   try {
//     // // 1. Fetch user profile (first name)
//     // const profileResponse = await api.get('/patientdashboard/user/profile')
//     // user.value = profileResponse.data

//     // // 2. Fetch dashboard data (exercises, next appointment, latest pain log)
//     // const dashboardResponse = await api.get('/patientdashboard')
//     // const data = dashboardResponse.data

//     // // Map exercises to match the structure that ExerciseCard expects
//     // if (data.exercises) {
//     //   todayExercises.value = data.exercises.map(e => ({
//     //     id: e.exercisePlanId,
//     //     exerciseId: e.exerciseId,
//     //     name: e.exerciseName,
//     //     description: e.description,
//     //     videoUrl: e.videoUrl,
//     //     sets: e.sets,
//     //     reps: e.repetitions,
//     //     frequencyPerDay: e.frequencyPerDay || 1,
//     //     startDate: e.startDate,
//     //     endDate: e.endDate,
//     //     completedCount: e.completedCount || 0
//     //   }))
//     // }

//     // // Map next appointment if available
//     // if (data.nextAppointment) {
//     //   const dt = new Date(data.nextAppointment.appointmentDateTime)
//     //   nextAppointment.value = {
//     //     doctor: data.nextAppointment.physiotherapistName || 'Fysiotherapeut',
//     //     type: data.nextAppointment.typeName,
//     //     date: dt.toLocaleDateString('nl-NL', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' }),
//     //     time: dt.toLocaleTimeString('nl-NL', { hour: '2-digit', minute: '2-digit' }) + ' uur'
//     //   }
//     // }

//     // // Map latest pain log if available
//     // if (data.latestPainLog) {
//     //   const dt = new Date(data.latestPainLog.registrationDateTime)
//     //   const score = data.latestPainLog.painScore
//     //   let label = 'Geen pijn'
//     //   if (score > 0 && score <= 3) label = 'Lichte pijn'
//     //   else if (score > 3 && score <= 6) label = 'Matige pijn'
//     //   else if (score > 6) label = 'Ernstige pijn'

//     //   latestPain.value = {
//     //     score: score,
//     //     label: label,
//     //     date: dt.toLocaleString('nl-NL', { dateStyle: 'medium', timeStyle: 'short' }),
//     //     note: data.latestPainLog.explanation || 'Geen opmerking toegevoegd.'
//     //   }
//     // }

//   } catch (error) {
//     console.error('Error loading dashboard data:', error)
//     errorMessage.value = 'Kon dashboardgegevens niet ophalen van de server.'
//   } finally {
//     isLoading.value = false
//   }
// })

</script>
