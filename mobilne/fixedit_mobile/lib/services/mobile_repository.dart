import 'package:dio/dio.dart';
import 'package:image_picker/image_picker.dart';

import '../core/api_client.dart';
import '../core/models.dart';

class HomeData {
  const HomeData(this.professionals, this.jobs);
  final List<Professional> professionals;
  final List<JobPosting> jobs;
}

class MobileRepository {
  const MobileRepository(this.api);
  final ApiClient api;

  Future<ReferenceData> getReferenceData() async {
    final results = await Future.wait([
      _getAllOptions('/api/reference-data/cities'),
      _getAllOptions('/api/reference-data/categories'),
    ]);
    return ReferenceData(results[0], results[1]);
  }

  Future<ReferenceData> getRegistrationReferenceData() async {
    final cities = await _getAllOptions('/api/auth/register/cities');
    return ReferenceData(cities, const []);
  }

  Future<List<LookupOption>> _getAllOptions(String path) async {
    final items = <LookupOption>[];
    var page = 1;
    Paged<LookupOption> result;
    do {
      final response = await api.call<Json>(
        () => api.dio.get<Json>(
          path,
          queryParameters: {'page': page, 'pageSize': 50},
        ),
      );
      result = Paged.fromJson(response.data ?? const {}, LookupOption.fromJson);
      items.addAll(result.items);
      page++;
    } while (page <= result.pageCount);
    return items;
  }

  Future<HomeData> getHome({required bool client}) async {
    final calls = <Future<Object>>[
      client ? getRecommendations() : getProfessionals(pageSize: 8),
      getJobs(pageSize: 8),
    ];
    final results = await Future.wait(calls);
    return HomeData(
      (results[0] as Paged<Professional>).items,
      (results[1] as Paged<JobPosting>).items,
    );
  }

  Future<Paged<Professional>> getRecommendations({int page = 1}) async {
    final r = await api.call<Json>(
      () => api.dio.get<Json>(
        '/api/recommendations',
        queryParameters: {'page': page, 'pageSize': 20},
      ),
    );
    return Paged.fromJson(r.data ?? const {}, Professional.fromJson);
  }

  Future<Paged<Professional>> getProfessionals({
    int page = 1,
    int pageSize = 20,
    String? name,
    int? cityId,
    int? categoryId,
    double? minRating,
    double? maxRate,
    String sortBy = 'rating',
  }) async {
    final searching =
        name != null ||
        cityId != null ||
        categoryId != null ||
        minRating != null ||
        maxRate != null ||
        sortBy != 'rating';
    final r = await api.call<Json>(
      () => api.dio.get<Json>(
        searching ? '/api/professionals/search' : '/api/professionals',
        queryParameters: {
          'page': page,
          'pageSize': pageSize,
          if (name != null && name.trim().isNotEmpty) 'name': name.trim(),
          'cityId': ?cityId,
          'categoryId': ?categoryId,
          'minRating': ?minRating,
          'maxHourlyRate': ?maxRate,
          'sortBy': sortBy,
          'sortOrder': 'desc',
        },
      ),
    );
    return Paged.fromJson(r.data ?? const {}, Professional.fromJson);
  }

  Future<Professional> getProfessional(int id) async {
    final r = await api.call<Json>(
      () => api.dio.get<Json>('/api/professionals/$id'),
    );
    return Professional.fromJson(r.data ?? const {});
  }

  Future<Paged<Review>> getReviews(int professionalId, {int page = 1}) async {
    final r = await api.call<Json>(
      () => api.dio.get<Json>(
        '/api/professionals/$professionalId/reviews',
        queryParameters: {'page': page, 'pageSize': 20},
      ),
    );
    return Paged.fromJson(r.data ?? const {}, Review.fromJson);
  }

  Future<Paged<JobPosting>> getJobs({
    int page = 1,
    int pageSize = 20,
    bool mine = false,
    int? cityId,
    int? categoryId,
  }) async {
    final searching = cityId != null || categoryId != null;
    final path = mine
        ? '/api/jobs/my'
        : searching
        ? '/api/jobs/search'
        : '/api/jobs';
    final r = await api.call<Json>(
      () => api.dio.get<Json>(
        path,
        queryParameters: {
          'page': page,
          'pageSize': pageSize,
          'cityId': ?cityId,
          'categoryId': ?categoryId,
        },
      ),
    );
    return Paged.fromJson(r.data ?? const {}, JobPosting.fromJson);
  }

  Future<JobPosting> createJob({
    required String title,
    required String description,
    required int cityId,
    required int categoryId,
    required double budget,
  }) async {
    final r = await api.call<Json>(
      () => api.dio.post<Json>(
        '/api/jobs',
        data: {
          'title': title.trim(),
          'description': description.trim(),
          'cityId': cityId,
          'categoryId': categoryId,
          'budget': budget,
        },
      ),
    );
    return JobPosting.fromJson(r.data ?? const {});
  }

  Future<void> addJobImage(int jobId, XFile image) async {
    final form = FormData.fromMap({
      'image': await MultipartFile.fromFile(image.path, filename: image.name),
    });
    await api.call<Json>(
      () => api.dio.post<Json>('/api/jobs/$jobId/images', data: form),
    );
  }

  Future<Paged<JobOffer>> getOffers(int jobId, {int page = 1}) async {
    final r = await api.call<Json>(
      () => api.dio.get<Json>(
        '/api/jobs/$jobId/offers',
        queryParameters: {'page': page, 'pageSize': 20},
      ),
    );
    return Paged.fromJson(r.data ?? const {}, JobOffer.fromJson);
  }

  Future<Paged<JobOffer>> getMyOffers({int page = 1}) async {
    final r = await api.call<Json>(
      () => api.dio.get<Json>(
        '/api/offers/my',
        queryParameters: {'page': page, 'pageSize': 20},
      ),
    );
    return Paged.fromJson(r.data ?? const {}, JobOffer.fromJson);
  }

  Future<JobOffer> submitOffer(int jobId, String message, double price) async {
    final r = await api.call<Json>(
      () => api.dio.post<Json>(
        '/api/jobs/$jobId/offers',
        data: {'message': message.trim(), 'proposedPrice': price},
      ),
    );
    return JobOffer.fromJson(r.data ?? const {});
  }

  Future<JobOffer> setOffer(int jobId, int offerId, bool accept) async {
    final r = await api.call<Json>(
      () => api.dio.put<Json>(
        '/api/jobs/$jobId/offers/$offerId/${accept ? 'accept' : 'reject'}',
      ),
    );
    return JobOffer.fromJson(r.data ?? const {});
  }

  Future<Reservation> createReservation(
    int professionalId,
    int categoryId,
    String description,
    DateTime scheduledAt,
    int duration,
  ) async {
    final r = await api.call<Json>(
      () => api.dio.post<Json>(
        '/api/reservations',
        data: {
          'professionalProfileId': professionalId,
          'categoryId': categoryId,
          'serviceDescription': description.trim(),
          'scheduledAt': scheduledAt.toUtc().toIso8601String(),
          'durationMinutes': duration,
        },
      ),
    );
    return Reservation.fromJson(r.data ?? const {});
  }

  Future<List<AvailableSlot>> getAvailableSlots({
    required int professionalId,
    required int categoryId,
    required DateTime date,
    required int durationMinutes,
  }) async {
    final response = await api.call<List<dynamic>>(
      () => api.dio.get<List<dynamic>>(
        '/api/professionals/$professionalId/available-slots',
        queryParameters: {
          'date':
              '${date.year.toString().padLeft(4, '0')}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}',
          'categoryId': categoryId,
          'durationMinutes': durationMinutes,
        },
      ),
    );
    return (response.data ?? const [])
        .whereType<Map>()
        .map((item) => AvailableSlot.fromJson(Map<String, dynamic>.from(item)))
        .toList();
  }

  Future<List<ProfessionalAvailability>> getMyAvailability() async {
    final response = await api.call<List<dynamic>>(
      () => api.dio.get<List<dynamic>>('/api/reservations/availability/my'),
    );
    return (response.data ?? const [])
        .whereType<Map>()
        .map(
          (item) => ProfessionalAvailability.fromJson(
            Map<String, dynamic>.from(item),
          ),
        )
        .toList();
  }

  Future<void> saveMyAvailability(Set<int> days) async {
    await api.call<List<dynamic>>(
      () => api.dio.put<List<dynamic>>(
        '/api/reservations/availability/my',
        data: {
          'periods': days
              .map(
                (day) => {
                  'dayOfWeek': day,
                  'startTime': '08:00:00',
                  'endTime': '17:00:00',
                },
              )
              .toList(),
        },
      ),
    );
  }

  Future<Paged<Reservation>> getReservations({int page = 1}) async {
    final r = await api.call<Json>(
      () => api.dio.get<Json>(
        '/api/reservations',
        queryParameters: {'page': page, 'pageSize': 30},
      ),
    );
    return Paged.fromJson(r.data ?? const {}, Reservation.fromJson);
  }

  Future<Reservation> getReservation(int id) async {
    final r = await api.call<Json>(
      () => api.dio.get<Json>('/api/reservations/$id'),
    );
    return Reservation.fromJson(r.data ?? const {});
  }

  Future<Reservation> transitionReservation(
    int id,
    String action, {
    String? reason,
  }) async {
    final r = await api.call<Json>(
      () => api.dio.put<Json>(
        '/api/reservations/$id/$action',
        data: action == 'cancel' ? {'reason': reason} : null,
      ),
    );
    return Reservation.fromJson(r.data ?? const {});
  }

  Future<Review> createReview(
    int reservationId,
    int rating,
    String comment,
  ) async {
    final r = await api.call<Json>(
      () => api.dio.post<Json>(
        '/api/reviews',
        data: {
          'reservationId': reservationId,
          'rating': rating,
          'comment': comment.trim(),
        },
      ),
    );
    return Review.fromJson(r.data ?? const {});
  }

  Future<Conversation> createConversationForReservation(
    int reservationId,
  ) async {
    final r = await api.call<Json>(
      () => api.dio.post<Json>(
        '/api/conversations',
        data: {'reservationId': reservationId},
      ),
    );
    return Conversation.fromJson(r.data ?? const {});
  }

  Future<Paged<ChatMessage>> getMessages(
    int conversationId, {
    int page = 1,
  }) async {
    final r = await api.call<Json>(
      () => api.dio.get<Json>(
        '/api/conversations/$conversationId/messages',
        queryParameters: {'page': page, 'pageSize': 50},
      ),
    );
    return Paged.fromJson(r.data ?? const {}, ChatMessage.fromJson);
  }

  Future<PaymentOrder> createPaymentOrder(int reservationId) async {
    final r = await api.call<Json>(
      () => api.dio.post<Json>(
        '/api/payments/create-order',
        data: {'reservationId': reservationId},
      ),
    );
    return PaymentOrder.fromJson(r.data ?? const {});
  }

  Future<void> capturePayment(String orderId) async {
    await api.call<Json>(
      () => api.dio.post<Json>('/api/payments/capture/$orderId'),
    );
  }

  Future<UserProfile> getProfile() async {
    final r = await api.call<Json>(
      () => api.dio.get<Json>('/api/users/profile'),
    );
    return UserProfile.fromJson(r.data ?? const {});
  }

  Future<UserProfile> updateProfile({
    required String email,
    required String firstName,
    required String lastName,
    required int cityId,
    String? phone,
  }) async {
    final r = await api.call<Json>(
      () => api.dio.put<Json>(
        '/api/users/profile',
        data: {
          'email': email.trim(),
          'firstName': firstName.trim(),
          'lastName': lastName.trim(),
          'phoneNumber': phone?.trim(),
          'cityId': cityId,
        },
      ),
    );
    return UserProfile.fromJson(r.data ?? const {});
  }

  Future<void> uploadProfilePicture(XFile image) async {
    final form = FormData.fromMap({
      'file': await MultipartFile.fromFile(image.path, filename: image.name),
    });
    await api.call<Json>(
      () => api.dio.post<Json>('/api/users/profile/picture', data: form),
    );
  }

  Future<Professional> getMyProfessionalProfile() async {
    final r = await api.call<Json>(
      () => api.dio.get<Json>('/api/professionals/my-profile'),
    );
    return Professional.fromJson(r.data ?? const {});
  }

  Future<Professional> updateProfessional({
    required String bio,
    required double rate,
    required int experience,
    required List<int> categoryIds,
  }) async {
    final r = await api.call<Json>(
      () => api.dio.put<Json>(
        '/api/professionals/my-profile',
        data: {
          'bio': bio.trim(),
          'hourlyRate': rate,
          'yearsOfExperience': experience,
          'categoryIds': categoryIds,
        },
      ),
    );
    return Professional.fromJson(r.data ?? const {});
  }

  Future<void> addPortfolio(
    String title,
    String description,
    XFile image,
  ) async {
    final form = FormData.fromMap({
      'title': title.trim(),
      'description': description.trim(),
      'image': await MultipartFile.fromFile(image.path, filename: image.name),
    });
    await api.call<Json>(
      () => api.dio.post<Json>('/api/professionals/portfolio', data: form),
    );
  }

  Future<void> deletePortfolio(int id) async {
    await api.call<void>(
      () => api.dio.delete<void>('/api/professionals/portfolio/$id'),
    );
  }

  Future<Paged<NotificationItem>> getNotifications({int page = 1}) async {
    final r = await api.call<Json>(
      () => api.dio.get<Json>(
        '/api/notifications',
        queryParameters: {'page': page, 'pageSize': 20},
      ),
    );
    return Paged.fromJson(r.data ?? const {}, NotificationItem.fromJson);
  }

  Future<int> getUnreadNotificationCount() async {
    final r = await api.call<int>(
      () => api.dio.get<int>('/api/notifications/unread-count'),
    );
    return r.data ?? 0;
  }

  Future<void> markNotificationRead(int id) async {
    await api.call<Json>(
      () => api.dio.put<Json>('/api/notifications/$id/read'),
    );
  }

  Future<void> markAllNotificationsRead() async {
    await api.call<void>(
      () => api.dio.put<void>('/api/notifications/read-all'),
    );
  }

  Future<EarningsSummary> getEarnings() async {
    final r = await api.call<Json>(
      () => api.dio.get<Json>(
        '/api/reports/financial',
        queryParameters: const {'page': 1, 'pageSize': 5},
      ),
    );
    return EarningsSummary.fromJson(r.data ?? const {});
  }
}
