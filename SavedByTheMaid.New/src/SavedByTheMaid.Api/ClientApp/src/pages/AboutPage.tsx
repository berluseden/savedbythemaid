import { Link } from 'react-router-dom';
import { Sparkles, Heart, Users, Award, Target } from 'lucide-react';

const team = [
  {
    name: 'Sarah Johnson',
    role: 'Founder & CEO',
    image: 'https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=400&h=400&fit=crop',
    bio: 'Founded SavedByTheMaid with a vision to make professional cleaning accessible to everyone.',
  },
  {
    name: 'Michael Chen',
    role: 'Head of Operations',
    image: 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=400&h=400&fit=crop',
    bio: 'Ensures every cleaning meets our high standards with over 10 years in service operations.',
  },
  {
    name: 'Emily Rodriguez',
    role: 'Customer Success Manager',
    image: 'https://images.unsplash.com/photo-1438761681033-6461ffad8d80?w=400&h=400&fit=crop',
    bio: 'Dedicated to making every customer experience exceptional from booking to completion.',
  },
];

const values = [
  {
    icon: Heart,
    title: 'Customer First',
    description: 'Every decision we make starts with our customers. Your satisfaction is our top priority.',
  },
  {
    icon: Award,
    title: 'Excellence',
    description: 'We never settle for "good enough." Every cleaning is performed to the highest standards.',
  },
  {
    icon: Users,
    title: 'Community',
    description: 'We invest in our cleaners and the communities we serve, building lasting relationships.',
  },
  {
    icon: Target,
    title: 'Reliability',
    description: 'When we say we\'ll be there, we\'ll be there. On time, every time, without exception.',
  },
];

const milestones = [
  { year: '2019', title: 'Founded', description: 'Started with just 3 cleaners and a dream' },
  { year: '2020', title: '1,000 Cleanings', description: 'Reached our first major milestone' },
  { year: '2021', title: 'Expanded', description: 'Grew to serve 5 major metropolitan areas' },
  { year: '2022', title: '10,000 Customers', description: 'Built a loyal community of happy homes' },
  { year: '2023', title: 'Award Winner', description: 'Named "Best Cleaning Service" by local press' },
  { year: '2024', title: 'Growing Strong', description: 'Continuing to expand with new services' },
];

export default function AboutPage() {
  return (
    <div className="min-h-screen bg-gray-50">
      {/* Hero Section */}
      <section className="bg-gradient-to-br from-sky-500 to-sky-600 text-white py-20">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h1 className="text-4xl md:text-5xl font-bold mb-4">About SavedByTheMaid</h1>
          <p className="text-xl text-sky-100 max-w-3xl mx-auto">
            We're on a mission to give people back their time by providing exceptional,
            reliable cleaning services they can trust.
          </p>
        </div>
      </section>

      {/* Story Section */}
      <section className="py-16 bg-white">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid lg:grid-cols-2 gap-12 items-center">
            <div>
              <h2 className="text-3xl font-bold text-gray-900 mb-6">Our Story</h2>
              <div className="space-y-4 text-gray-600">
                <p>
                  SavedByTheMaid was born from a simple observation: people were spending precious
                  weekend hours cleaning instead of enjoying time with family and friends. We knew
                  there had to be a better way.
                </p>
                <p>
                  In 2019, we launched with a small team of dedicated cleaning professionals and a
                  commitment to making professional cleaning accessible, affordable, and reliable
                  for everyone.
                </p>
                <p>
                  Today, we've helped thousands of families reclaim their time while maintaining
                  spotless homes. Our vetted, insured cleaners treat every home like their own,
                  and our 100% satisfaction guarantee means you can book with confidence.
                </p>
              </div>
            </div>
            <div className="relative">
              <img
                src="https://images.unsplash.com/photo-1581578731548-c64695cc6952?w=600&h=400&fit=crop"
                alt="Professional cleaner at work"
                className="rounded-2xl shadow-lg"
              />
              <div className="absolute -bottom-6 -left-6 bg-sky-500 text-white p-6 rounded-xl shadow-lg">
                <div className="text-4xl font-bold">5+</div>
                <div className="text-sky-100">Years of Excellence</div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Values Section */}
      <section className="py-16">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-12">
            <h2 className="text-3xl font-bold text-gray-900 mb-4">Our Core Values</h2>
            <p className="text-gray-600 max-w-2xl mx-auto">
              These principles guide everything we do, from hiring our team to serving our customers.
            </p>
          </div>

          <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-8">
            {values.map((value, idx) => (
              <div key={idx} className="bg-white rounded-xl p-6 shadow-sm border border-gray-200">
                <div className="w-12 h-12 bg-sky-100 rounded-xl flex items-center justify-center mb-4">
                  <value.icon className="w-6 h-6 text-sky-600" />
                </div>
                <h3 className="text-lg font-semibold text-gray-900 mb-2">{value.title}</h3>
                <p className="text-gray-600 text-sm">{value.description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Stats Section */}
      <section className="py-16 bg-sky-500">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-8 text-center text-white">
            <div>
              <div className="text-4xl md:text-5xl font-bold mb-2">10,000+</div>
              <div className="text-sky-100">Happy Customers</div>
            </div>
            <div>
              <div className="text-4xl md:text-5xl font-bold mb-2">50,000+</div>
              <div className="text-sky-100">Cleanings Completed</div>
            </div>
            <div>
              <div className="text-4xl md:text-5xl font-bold mb-2">100+</div>
              <div className="text-sky-100">Professional Cleaners</div>
            </div>
            <div>
              <div className="text-4xl md:text-5xl font-bold mb-2">4.9★</div>
              <div className="text-sky-100">Average Rating</div>
            </div>
          </div>
        </div>
      </section>

      {/* Timeline Section */}
      <section className="py-16 bg-white">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-12">
            <h2 className="text-3xl font-bold text-gray-900 mb-4">Our Journey</h2>
            <p className="text-gray-600">From humble beginnings to industry leader</p>
          </div>

          <div className="relative">
            <div className="absolute left-1/2 transform -translate-x-1/2 w-0.5 h-full bg-sky-200" />
            <div className="space-y-8">
              {milestones.map((milestone, idx) => (
                <div
                  key={idx}
                  className={`flex items-center gap-8 ${idx % 2 === 0 ? 'flex-row' : 'flex-row-reverse'}`}
                >
                  <div className={`flex-1 ${idx % 2 === 0 ? 'text-right' : 'text-left'}`}>
                    <div className="bg-gray-50 rounded-lg p-4 inline-block">
                      <div className="text-sky-600 font-bold">{milestone.year}</div>
                      <div className="font-semibold text-gray-900">{milestone.title}</div>
                      <div className="text-sm text-gray-600">{milestone.description}</div>
                    </div>
                  </div>
                  <div className="relative z-10 w-4 h-4 bg-sky-500 rounded-full border-4 border-white shadow" />
                  <div className="flex-1" />
                </div>
              ))}
            </div>
          </div>
        </div>
      </section>

      {/* Team Section */}
      <section className="py-16">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-12">
            <h2 className="text-3xl font-bold text-gray-900 mb-4">Meet Our Leadership</h2>
            <p className="text-gray-600">The passionate people behind SavedByTheMaid</p>
          </div>

          <div className="grid md:grid-cols-3 gap-8">
            {team.map((member, idx) => (
              <div key={idx} className="bg-white rounded-xl overflow-hidden shadow-sm border border-gray-200">
                <img
                  src={member.image}
                  alt={member.name}
                  className="w-full h-64 object-cover"
                />
                <div className="p-6">
                  <h3 className="text-lg font-semibold text-gray-900">{member.name}</h3>
                  <p className="text-sky-600 text-sm mb-3">{member.role}</p>
                  <p className="text-gray-600 text-sm">{member.bio}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="py-16 bg-gradient-to-br from-sky-500 to-sky-600">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h2 className="text-3xl font-bold text-white mb-4">Ready to Experience the Difference?</h2>
          <p className="text-sky-100 mb-8">
            Join thousands of happy customers who've discovered the SavedByTheMaid difference.
          </p>
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <Link
              to="/book"
              className="inline-flex items-center justify-center gap-2 bg-white text-sky-600 px-8 py-4 rounded-xl font-semibold hover:bg-gray-100 transition-colors"
            >
              <Sparkles className="w-5 h-5" />
              Book Your First Cleaning
            </Link>
            <Link
              to="/contact"
              className="inline-flex items-center justify-center gap-2 border-2 border-white text-white px-8 py-4 rounded-xl font-semibold hover:bg-white/10 transition-colors"
            >
              Contact Us
            </Link>
          </div>
        </div>
      </section>
    </div>
  );
}
