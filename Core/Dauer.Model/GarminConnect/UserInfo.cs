using System.Text.Json.Serialization;

namespace Dauer.Model.GarminConnect;

/// <summary>
/// User info
/// </summary>
public class UserInfo
{
  /// <summary>
  /// Gets or sets the identifier.
  /// </summary>
  /// <value>
  /// The identifier.
  /// </value>
  [JsonPropertyName("id")]
  public long Id { get; set; }

  /// <summary>
  /// Gets or sets the profile identifier.
  /// </summary>
  /// <value>
  /// The profile identifier.
  /// </value>
  [JsonPropertyName("profileId")]
  public long ProfileId { get; set; }

  /// <summary>
  /// Gets or sets the garmin unique identifier.
  /// </summary>
  /// <value>
  /// The garmin unique identifier.
  /// </value>
  [JsonPropertyName("garminGUID")]
  public Guid GarminGuid { get; set; }

  /// <summary>
  /// Gets or sets the display name.
  /// </summary>
  /// <value>
  /// The display name.
  /// </value>
  [JsonPropertyName("displayName")]
  public Guid DisplayName { get; set; }

  /// <summary>
  /// Gets or sets the full name.
  /// </summary>
  /// <value>
  /// The full name.
  /// </value>
  [JsonPropertyName("fullName")]
  public string FullName { get; set; }

  /// <summary>
  /// Gets or sets the name of the user.
  /// </summary>
  /// <value>
  /// The name of the user.
  /// </value>
  [JsonPropertyName("userName")]
  public string UserName { get; set; }

  /// <summary>
  /// Gets or sets the profile image URL large.
  /// </summary>
  /// <value>
  /// The profile image URL large.
  /// </value>
  [JsonPropertyName("profileImageUrlLarge")]
  public Uri ProfileImageUrlLarge { get; set; }

  /// <summary>
  /// Gets or sets the profile image URL medium.
  /// </summary>
  /// <value>
  /// The profile image URL medium.
  /// </value>
  [JsonPropertyName("profileImageUrlMedium")]
  public Uri ProfileImageUrlMedium { get; set; }

  /// <summary>
  /// Gets or sets the profile image URL small.
  /// </summary>
  /// <value>
  /// The profile image URL small.
  /// </value>
  [JsonPropertyName("profileImageUrlSmall")]
  public Uri ProfileImageUrlSmall { get; set; }

  /// <summary>
  /// Gets or sets the location.
  /// </summary>
  /// <value>
  /// The location.
  /// </value>
  [JsonPropertyName("location")]
  public object Location { get; set; }

  /// <summary>
  /// Gets or sets the facebook URL.
  /// </summary>
  /// <value>
  /// The facebook URL.
  /// </value>
  [JsonPropertyName("facebookUrl")]
  public object FacebookUrl { get; set; }

  /// <summary>
  /// Gets or sets the twitter URL.
  /// </summary>
  /// <value>
  /// The twitter URL.
  /// </value>
  [JsonPropertyName("twitterUrl")]
  public object TwitterUrl { get; set; }

  /// <summary>
  /// Gets or sets the personal website.
  /// </summary>
  /// <value>
  /// The personal website.
  /// </value>
  [JsonPropertyName("personalWebsite")]
  public object PersonalWebsite { get; set; }

  /// <summary>
  /// Gets or sets the motivation.
  /// </summary>
  /// <value>
  /// The motivation.
  /// </value>
  [JsonPropertyName("motivation")]
  public long Motivation { get; set; }

  /// <summary>
  /// Gets or sets the bio.
  /// </summary>
  /// <value>
  /// The bio.
  /// </value>
  [JsonPropertyName("bio")]
  public object Bio { get; set; }

  /// <summary>
  /// Gets or sets the primary activity.
  /// </summary>
  /// <value>
  /// The primary activity.
  /// </value>
  [JsonPropertyName("primaryActivity")]
  public string PrimaryActivity { get; set; }

  /// <summary>
  /// Gets or sets the favorite activity types.
  /// </summary>
  /// <value>
  /// The favorite activity types.
  /// </value>
  [JsonPropertyName("favoriteActivityTypes")]
  public string[] FavoriteActivityTypes { get; set; }

  /// <summary>
  /// Gets or sets the running training speed.
  /// </summary>
  /// <value>
  /// The running training speed.
  /// </value>
  [JsonPropertyName("runningTrainingSpeed")]
  public double RunningTrainingSpeed { get; set; }

  /// <summary>
  /// Gets or sets the cycling training speed.
  /// </summary>
  /// <value>
  /// The cycling training speed.
  /// </value>
  [JsonPropertyName("cyclingTrainingSpeed")]
  public long CyclingTrainingSpeed { get; set; }

  /// <summary>
  /// Gets or sets the favorite cycling activity types.
  /// </summary>
  /// <value>
  /// The favorite cycling activity types.
  /// </value>
  [JsonPropertyName("favoriteCyclingActivityTypes")]
  public string[] FavoriteCyclingActivityTypes { get; set; }

  /// <summary>
  /// Gets or sets the cycling classification.
  /// </summary>
  /// <value>
  /// The cycling classification.
  /// </value>
  [JsonPropertyName("cyclingClassification")]
  public string CyclingClassification { get; set; }

  /// <summary>
  /// Gets or sets the cycling maximum average power.
  /// </summary>
  /// <value>
  /// The cycling maximum average power.
  /// </value>
  [JsonPropertyName("cyclingMaxAvgPower")]
  public long CyclingMaxAvgPower { get; set; }

  /// <summary>
  /// Gets or sets the swimming training speed.
  /// </summary>
  /// <value>
  /// The swimming training speed.
  /// </value>
  [JsonPropertyName("swimmingTrainingSpeed")]
  public double SwimmingTrainingSpeed { get; set; }

  /// <summary>
  /// Gets or sets the profile visibility.
  /// </summary>
  /// <value>
  /// The profile visibility.
  /// </value>
  [JsonPropertyName("profileVisibility")]
  public string ProfileVisibility { get; set; }

  /// <summary>
  /// Gets or sets the activity start visibility.
  /// </summary>
  /// <value>
  /// The activity start visibility.
  /// </value>
  [JsonPropertyName("activityStartVisibility")]
  public string ActivityStartVisibility { get; set; }

  /// <summary>
  /// Gets or sets the activity map visibility.
  /// </summary>
  /// <value>
  /// The activity map visibility.
  /// </value>
  [JsonPropertyName("activityMapVisibility")]
  public string ActivityMapVisibility { get; set; }

  /// <summary>
  /// Gets or sets the course visibility.
  /// </summary>
  /// <value>
  /// The course visibility.
  /// </value>
  [JsonPropertyName("courseVisibility")]
  public string CourseVisibility { get; set; }

  /// <summary>
  /// Gets or sets the activity heart rate visibility.
  /// </summary>
  /// <value>
  /// The activity heart rate visibility.
  /// </value>
  [JsonPropertyName("activityHeartRateVisibility")]
  public string ActivityHeartRateVisibility { get; set; }

  /// <summary>
  /// Gets or sets the activity power visibility.
  /// </summary>
  /// <value>
  /// The activity power visibility.
  /// </value>
  [JsonPropertyName("activityPowerVisibility")]
  public string ActivityPowerVisibility { get; set; }

  /// <summary>
  /// Gets or sets the badge visibility.
  /// </summary>
  /// <value>
  /// The badge visibility.
  /// </value>
  [JsonPropertyName("badgeVisibility")]
  public string BadgeVisibility { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [show age].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [show age]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("showAge")]
  public bool ShowAge { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [show weight].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [show weight]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("showWeight")]
  public bool ShowWeight { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [show height].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [show height]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("showHeight")]
  public bool ShowHeight { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [show weight class].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [show weight class]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("showWeightClass")]
  public bool ShowWeightClass { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [show age range].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [show age range]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("showAgeRange")]
  public bool ShowAgeRange { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [show gender].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [show gender]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("showGender")]
  public bool ShowGender { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [show activity class].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [show activity class]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("showActivityClass")]
  public bool ShowActivityClass { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [show vo2 maximum].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [show vo2 maximum]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("showVO2Max")]
  public bool ShowVo2Max { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [show personal records].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [show personal records]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("showPersonalRecords")]
  public bool ShowPersonalRecords { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [show last12 months].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [show last12 months]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("showLast12Months")]
  public bool ShowLast12Months { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [show lifetime totals].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [show lifetime totals]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("showLifetimeTotals")]
  public bool ShowLifetimeTotals { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [show upcoming events].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [show upcoming events]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("showUpcomingEvents")]
  public bool ShowUpcomingEvents { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [show recent favorites].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [show recent favorites]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("showRecentFavorites")]
  public bool ShowRecentFavorites { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [show recent device].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [show recent device]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("showRecentDevice")]
  public bool ShowRecentDevice { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [show recent gear].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [show recent gear]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("showRecentGear")]
  public bool ShowRecentGear { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [show badges].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [show badges]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("showBadges")]
  public bool ShowBadges { get; set; }

  /// <summary>
  /// Gets or sets the other activity.
  /// </summary>
  /// <value>
  /// The other activity.
  /// </value>
  [JsonPropertyName("otherActivity")]
  public object OtherActivity { get; set; }

  /// <summary>
  /// Gets or sets the other primary activity.
  /// </summary>
  /// <value>
  /// The other primary activity.
  /// </value>
  [JsonPropertyName("otherPrimaryActivity")]
  public object OtherPrimaryActivity { get; set; }

  /// <summary>
  /// Gets or sets the other motivation.
  /// </summary>
  /// <value>
  /// The other motivation.
  /// </value>
  [JsonPropertyName("otherMotivation")]
  public object OtherMotivation { get; set; }

  /// <summary>
  /// Gets or sets the user roles.
  /// </summary>
  /// <value>
  /// The user roles.
  /// </value>
  [JsonPropertyName("userRoles")]
  public string[] UserRoles { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [name approved].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [name approved]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("nameApproved")]
  public bool NameApproved { get; set; }

  /// <summary>
  /// Gets or sets the full name of the user profile.
  /// </summary>
  /// <value>
  /// The full name of the user profile.
  /// </value>
  [JsonPropertyName("userProfileFullName")]
  public string UserProfileFullName { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [make golf scorecards private].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [make golf scorecards private]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("makeGolfScorecardsPrivate")]
  public bool MakeGolfScorecardsPrivate { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [allow golf live scoring].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [allow golf live scoring]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("allowGolfLiveScoring")]
  public bool AllowGolfLiveScoring { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [allow golf scoring by connections].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [allow golf scoring by connections]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("allowGolfScoringByConnections")]
  public bool AllowGolfScoringByConnections { get; set; }

  /// <summary>
  /// Gets or sets the user level.
  /// </summary>
  /// <value>
  /// The user level.
  /// </value>
  [JsonPropertyName("userLevel")]
  public long UserLevel { get; set; }

  /// <summary>
  /// Gets or sets the user point.
  /// </summary>
  /// <value>
  /// The user point.
  /// </value>
  [JsonPropertyName("userPoint")]
  public long UserPoint { get; set; }

  /// <summary>
  /// Gets or sets the level update date.
  /// </summary>
  /// <value>
  /// The level update date.
  /// </value>
  [JsonPropertyName("levelUpdateDate")]
  public DateTimeOffset LevelUpdateDate { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [level is viewed].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [level is viewed]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("levelIsViewed")]
  public bool LevelIsViewed { get; set; }

  /// <summary>
  /// Gets or sets the level point threshold.
  /// </summary>
  /// <value>
  /// The level point threshold.
  /// </value>
  [JsonPropertyName("levelPointThreshold")]
  public long LevelPointThreshold { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [user pro].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [user pro]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("userPro")]
  public bool UserPro { get; set; }
}